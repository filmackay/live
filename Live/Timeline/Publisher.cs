using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public class Actions
    {
        private List<Action> _actions;

        public void Add(Action action)
        {
            using (this.Lock())
            {
                if (_actions != null)
                {
                    _actions.Add(action);
                    return;
                }
            }

            action();
        }

        public IList<Action> Process(Action process)
        {
            using (this.Lock())
            {
                if (_actions != null)
                    throw new InvalidOperationException("Processing already running");
                _actions = new List<Action>();
            }

            process();

            using (this.Lock())
            {
                var actions = _actions;
                _actions = null;
                return actions;
            }
        }
    }

    public enum TransactionStatus
    {
        None,
        Transaction,
        Committing,
    }

    public class Publisher
    {
        public static Publisher Global = new Publisher();

        protected readonly ReadWriteLockable _commitLock = new ReadWriteLockable();
        protected readonly NotifyList<Action> _onCommit = new NotifyList<Action>();
        protected readonly Actions _afterCommit = new Actions();

        public TransactionStatus Status
        {
            get { return (TransactionStatus)_commitLock.Status; }
        }

        public enum CommitResult
        {
            CouldNotGetLocks,
            ProcessedNotifications,
            NoNotifications,
        }

        private static readonly Action<IList<Action>, int, int> _runRange = (actions, from, to) =>
        {
            for (var i = from; i < to; i++)
                actions[i]();
        };

        protected CommitResult Commit(bool block = true)
        {
            if (!_onCommit.Notified)
                return CommitResult.NoNotifications;

            var result = CommitResult.NoNotifications;
            IList<Action> afterCommitActions = null;
            using (var @lock = _commitLock.WriteLock(block))
            {
                if (@lock == null)
                    return CommitResult.CouldNotGetLocks;

                _onCommit.ProcessResult(actions =>
                {
                    if (actions == null)
                        return;

                    // process commit as quickly as possible
                    afterCommitActions = _afterCommit.Process(() => actions.FastParallel(_runRange));

                    result = CommitResult.ProcessedNotifications;
                });
            }

            // start post-processing
            afterCommitActions.FastParallel(_runRange);

            return result;
        }

        public IDisposable ReadLock(bool block = true)
        { return _commitLock.ReadLock(block); }

        public void Run(Action<Action> action)
        {
            var readLock = _commitLock.ReadLock();

            // run action using our publishing context
            action(() =>
                {
                    // commit changes
                    readLock.Dispose();
                    Commit();
                    readLock = _commitLock.ReadLock();
                });

            // finish publishing
            readLock.Dispose();

            // try to commit
            Commit(false);
        }

        public void NotifyOnCommit(Action action)
        {
            while (true)
            {
                var generation = _onCommit.Generation;
                if (_commitLock.CurrentReaders == 0)
                {
                    // running outside transaction
                    action();
                    return;
                }

                // inside transaction - add, if nothing has changed
                if (_onCommit.Add(action, generation))
                    return;
            }
        }

        public void NotifyAfterCommit(Action action)
        {
            // this will run the action immediately if we are outside a commit, or queue it for after the commit
            _afterCommit.Add(action);
        }
    }
}