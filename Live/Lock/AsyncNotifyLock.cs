using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    [DebuggerDisplay("{Status}")]
    public class AsyncNotifyLock<T>
    {
        public string Name;
        public bool Trace;
        private readonly AsyncSemaphore _process = new AsyncSemaphore(1);
        private NotifyStatus _status;
        private TaskCompletionSource<T> _processCompleted = new TaskCompletionSource<T>(); // tells notifier of the completion of processing
        private readonly Subject<Unit> _onNotify = new Subject<Unit>();
        public IObservable<Unit> OnNotify { get { return _onNotify; } }

        public NotifyStatus Status
        {
            get { return _status; }
        }

        public Task<T> Notify()
        {
            // TODO: should we test for notification outside lock for optimisation?

            // get lock
            using (this.Lock())
            {
                var processCompleted = _processCompleted;
                switch (Status)
                {
                    case NotifyStatus.None:
                        // start new notification
                        if (Trace)
                            Debug.Print("{0} #{1}: Notifying", Name, GetHashCode());

                        // setup notify to inform this and other notifiers on completion
                        _status = NotifyStatus.Notified;

                        // inform
                        _onNotify.OnNext(new Unit());
                        break;

                    case NotifyStatus.Notified:
                        // current processing
                        if (Trace)
                            Debug.Print("{0} #{1}: already notified", Name, GetHashCode());
                        break;

                    case NotifyStatus.Processing:
                    case NotifyStatus.ProcessingNotified:
                        // next processing
                        if (Trace)
                            Debug.Print("{0} #{1}: already processing", Name, GetHashCode());
                        if (Status == NotifyStatus.Processing)
                            _status = NotifyStatus.ProcessingNotified;
                        break;
                }

                return processCompleted.Task;
            }
        }

        public Task Process(Func<Task> process)
        {
            return Process(async () =>
                {
                    await process();
                    return default(T);
                });
        }

        public async Task Process(Func<Task<T>> process)
        {
            // only one processor at a time
            using (await _process.Lock())
            {
                if (Trace)
                    Debug.Print("{0} #{1}: Processing", Name, GetHashCode());

                Debug.Assert(_status <= NotifyStatus.Notified);

                // start processing
                TaskCompletionSource<T> processCompleted;
                using (this.Lock())
                {
                    // swap completion subjects
                    processCompleted = _processCompleted;
                    _processCompleted = new TaskCompletionSource<T>();
                    _status = NotifyStatus.Processing;
                }

                // do processing
                var result = await process();

                // processing completed
                if (Trace)
                    Debug.Print("{0} #{2}: finish processing (thread #{1})", Name, Thread.CurrentThread.ManagedThreadId, GetHashCode());

                NotifyStatus oldStatus;
                using (this.Lock())
                {
                    oldStatus = _status;
                    switch (_status)
                    {
                        case NotifyStatus.ProcessingNotified:
                            _status = NotifyStatus.Notified;
                            break;
                        case NotifyStatus.Processing:
                            _status = NotifyStatus.None;
                            break;

                        default:
                            throw new InvalidOperationException("Invalid NotifyLock state");
                    }
                }

                // inform all notifiers processing is complete
                processCompleted.SetResult(result);

                // renotify asynchronously if we were notified during processing
                if (oldStatus == NotifyStatus.ProcessingNotified && OnNotify != null)
                    Task.Factory.StartNew(() => _onNotify.OnNext(new Unit()));
            }
        }
    }

    public class AsyncNotifyLock : AsyncNotifyLock<Unit>
    {
    }
}
