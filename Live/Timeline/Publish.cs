using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;


namespace Vertigo.Live
{
    public enum PublishStatus
    {
        None,
        Consume,
        Publish,
        PublishAndConsume,
    }

    public static class Publish
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(Publish));
        public static readonly ReaderWriterSpinLock _publishLock = new ReaderWriterSpinLock(); // read=Consume, write=Publish
        private static readonly LockFreeQueue<Action> _onPublish = new LockFreeQueue<Action>(); // handled by calls to Commit()
        private static readonly LockFreeQueue<Action> _onPublishConsume = new LockFreeQueue<Action>();
        private static readonly NotifyList<Action> _onConsume;
        private static TaskCompletionSource<Unit> _commitCompletion;

        static Publish()
        {
            //_commitLock.ID = -1;
            _onConsume = new NotifyList<Action> // handled on-demand
                {
                    OnNotify = () =>
                        {
                            // impossible for Consume lock to not be available, since _onConsume gets paused while Publish lock is held
                            Debug.Assert(!_publishLock.IsWriteLockHeld);
                            using (Transaction())
                                _onConsume.Get().ForEach();
                        },
                };
        }

        private static PublishStatus _publishStatus;

        public static bool AllowConsume
        {
            get { return _publishStatus == PublishStatus.PublishAndConsume || _publishLock.CurrentReadCount > 0; }
        }

        public static bool AllowPublish
        {
            get { return _publishStatus == PublishStatus.PublishAndConsume || _publishStatus == PublishStatus.Publish; }
        }

        //public static IDisposable TransactionFlush(bool block = true)
        //{
        //    // start transaction
        //    var readLock = _publishLock.ReadLock(block);
        //    if (readLock == null)
        //        return null;
        //    var start = HiResTimer.Now;
        //    return Disposable.Create(() => EndTransaction(start, readLock).Wait());
        //}

        public static IDisposable Transaction(bool block = true)
        {
            // start transaction
            var readLock = _publishLock.ReadLock(block);
            if (readLock == null)
                return null;
            var start = HiResTimer.Now();
            return Disposable.Create(() => EndTransaction(start, readLock));
        }

        public static Task EndTransaction(long start, ILock consumelock)
        {
            return Commit(consumelock.Release());
        }

        public static Task Commit(bool noConsumers)
        {
            // commit already in progress?
            var completion = _commitCompletion;
            while (true)
            {
                completion = _commitCompletion;
                if (completion != null)
                    break;
                completion = new TaskCompletionSource<Unit>();
                if (Interlocked.CompareExchange(ref _commitCompletion, completion, null) == null)
                    break;
            }

            // leave the commit to the final consumer
            if (noConsumers)
            {
                // get locks in order to make sure we never publish without consumption being paused
                using (_onConsume.PauseNotifications())
                using (var publishLock = _publishLock.WriteLock())
                {
                    // stop handing uot completion
                    _commitCompletion = null;

                    var loops = 0;
                    while (true)
                    {
                        loops++;

                        // run publish actions
                        _publishStatus = PublishStatus.Publish;
                        _onPublish.GetAll().ForEach();
                        _publishStatus = PublishStatus.PublishAndConsume;

                        // keep read-lock so that other commits will not be attempted within here
                        publishLock.DowngradeToReader();

                        // handle any Consumers that occurred during Publish

                        // run post-publish actions
                        _onPublishConsume.GetAll().ForEach();
                        _publishStatus = PublishStatus.None;

                        // are there publishers triggered from these consumers?
                        if (_onPublish.IsEmpty)
                            break;

                        // go again
                        publishLock.UpgradeToWriter();
                    }

                    if (loops > 1)
                        Log.Info("Commit loops: {0}", loops);
                }
            }

            return completion.Task;
        }

        public static Task Transaction(Action action)
        {
            var readLock = _publishLock.ReadLock();
            var start = HiResTimer.Now();
            action();
            return EndTransaction(start, readLock);
        }

        public static void OnPublish(Action action)
        {
            // action is run during the Commit() publishing
            Debug.Assert(AllowConsume);
            _onPublish.Enqueue(action);
        }

        public static void OnPublishConsume(Action action)
        {
            // run action after all OnPublish actions, but before OnConsume actions
            // used by part-publisher/consumer
            switch (_publishStatus)
            {
                case PublishStatus.Publish:
                    _onPublishConsume.Enqueue(action);
                    break;
                case PublishStatus.PublishAndConsume:
                    action();
                    break;
                default:
                    throw new InvalidOperationException("Publishing must be allowed");
            }
        }

        public static void OnConsume(Action action, bool block = false)
        {
            // try to get a read lock
            using (var transaction = Transaction(block))
            {
                if (transaction != null)
                    // another thread is not publishing, so we can run it now
                    action();
                else
                {
                    // another thread is publishing, we need to run this later
                    // eg. this handles notifications to State() as they always happen during Publish
                    _onConsume.Add(action);
                }
            }
        }
    }
}
