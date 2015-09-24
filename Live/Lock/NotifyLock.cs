using System;
using System.Diagnostics;
using System.Threading;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public enum NotifyStatus
    {
        None,
        Notified,
        Paused,
        PausedNotified,
        PreProcessing,              // notifications have no effect since we are about to process but have not started
        Processing,                 // notifications will be re-processed
        ProcessingNotified,         // processing will be run again after completion of this processing
    }

    public static partial class Extensions
    {
        public static bool IsNotified(this NotifyStatus status)
        {
            return status == NotifyStatus.Notified || status == NotifyStatus.ProcessingNotified;
        }

        public static bool IsProcessing(this NotifyStatus status)
        {
            return status == NotifyStatus.Processing || status == NotifyStatus.ProcessingNotified;
        }
    }

    [DebuggerDisplay("{Status}")]
    public class NotifyLock
    {
        public string Name;
        public bool Trace;

        public NotifyStatus Status
        {
            get { return (NotifyStatus)_status; }
        }

        private NotifyStatus ModifyStatus(NotifyStatus[] map)
        {
            int oldStatus, newStatus;
            do
            {
                oldStatus = _status;
                newStatus = (int)map[_status];
            } while (Interlocked.CompareExchange(ref _status, newStatus, oldStatus) != oldStatus);

            return (NotifyStatus)oldStatus;
        }

        private NotifyStatus StatusCompareExchange(NotifyStatus newValue, NotifyStatus oldvalue)
        {
            return (NotifyStatus)Interlocked.CompareExchange(ref _status, (int)newValue, (int)oldvalue);
        }

        public bool Notify()
        {
            // notify?
            var oldStatus = ModifyStatus(_notifyMap);

            // handle outcome
            switch (oldStatus)
            {
                case NotifyStatus.None:
                    if (Trace)
                        Debug.Print("{0} #{1}: notified", Name, GetHashCode());
                    if (OnNotify != null)
                        OnNotify();
                    break;
                case NotifyStatus.Processing:
                    if (Trace)
                        Debug.Print("{0} #{1}: notified while processing{2}", Name, GetHashCode(), _processingThread == Thread.CurrentThread.ManagedThreadId ? " on same thread" : "");
                    break;
                case NotifyStatus.Paused:
                    if (Trace)
                        Debug.Print("{0} #{1}: notified while paused", Name, GetHashCode());
                    break;
                default:
                    return false;
            }
            return true;
        }
        private static readonly NotifyStatus[] _notifyMap =
        {
            NotifyStatus.Notified,
            NotifyStatus.Notified,
            NotifyStatus.PausedNotified,
            NotifyStatus.PausedNotified,
            NotifyStatus.PreProcessing,
            NotifyStatus.ProcessingNotified,
            NotifyStatus.ProcessingNotified,
        };

        public IDisposable PauseNotifications(bool block = true)
        {
            var spinWait = default(SpinWait);

            while (true)
            {
                // notify?
                var oldStatus = StatusCompareExchange(NotifyStatus.Paused, NotifyStatus.None);

                // handle outcome
                if (oldStatus == NotifyStatus.None)
                {
                    if (Trace)
                        Debug.Print("{0} #{1}: Paused", Name, GetHashCode());
                    _pauseThread = Thread.CurrentThread.ManagedThreadId;
                    return Disposable.Create(() =>
                        {
                            _pauseThread = 0;
                            oldStatus = (NotifyStatus)Concurrency.Atomic(ref _status,
                                old =>
                                {
                                    switch ((NotifyStatus)old)
                                    {
                                        case NotifyStatus.PausedNotified:
                                            return (int)NotifyStatus.Notified;
                                        case NotifyStatus.Paused:
                                            return (int)NotifyStatus.None;
                                    }
                                    return old;
                                });

                            if (oldStatus == NotifyStatus.PausedNotified)
                                if (OnNotify != null)
                                    OnNotify();
                        });
                }

                if (!block)
                    return null;

                // spin
                spinWait.SpinOnce();
            }
        }

        public bool Unnotify()
        {
            // removes any notification, if there is one
            var oldStatus = ModifyStatus(_unnotifyMap);

            // return if we found a notification
            return
                oldStatus == NotifyStatus.Notified ||
                oldStatus == NotifyStatus.PausedNotified ||
                oldStatus == NotifyStatus.ProcessingNotified;
        }
        private static readonly NotifyStatus[] _unnotifyMap =
        {
            NotifyStatus.None,
            NotifyStatus.None,
            NotifyStatus.Paused,
            NotifyStatus.Paused,
            NotifyStatus.PreProcessing,
            NotifyStatus.Processing,
            NotifyStatus.Processing,
        };

        public void Process(Action<bool> process)
        {
            Process((notified, commit) =>
                {
                    commit();
                    process(notified);
                });
        }

        public Tuple<bool, Func<Action>> ProcessEx()
        {
            var spinWait = default(SpinWait);

            NotifyStatus oldStatus;
            while (true)
            {
                // get lock
                oldStatus = ModifyStatus(_processMap);

                // got lock?
                if (oldStatus <= NotifyStatus.Notified)
                    break;

                // recursive processing?
                if (_processingThread == Thread.CurrentThread.ManagedThreadId)
                    return
                        Tuple.Create<bool, Func<Action>>(
                            oldStatus == NotifyStatus.Notified,
                            () => () => { });

                // processing in another thread - wait
                spinWait.SpinOnce();
            }

            // process
            if (Trace)
                Debug.Print("{0} #{2}: pre processing (thread #{1})", Name, Thread.CurrentThread.ManagedThreadId, GetHashCode());
            _processingThread = Thread.CurrentThread.ManagedThreadId;

            // callback is to commit to the processing
            return
                Tuple.Create<bool, Func<Action>>(
                    oldStatus == NotifyStatus.Notified,
                    () =>
                    {
                        // commit
                        _status = (int)NotifyStatus.Processing;
                        return
                            () =>
                            {
                                // processing done
                                _processingThread = 0;

                                // release lock
                                if (Trace)
                                    Debug.Print("{0} #{2}: finish processing (thread #{1})", Name, Thread.CurrentThread.ManagedThreadId, GetHashCode());
                                oldStatus = ModifyStatus(_doneMap);

                                // renotify asynchronously if we were notified during processing
                                if (oldStatus == NotifyStatus.ProcessingNotified && OnNotify != null)
                                {
                                    Debug.Print("NotifyLock: ProcessingNotified");
                                    OnNotify();
                                }
                            };
                    });
        }
        private static readonly NotifyStatus[] _processMap =
        {
            NotifyStatus.PreProcessing,
            NotifyStatus.PreProcessing,
            NotifyStatus.Paused,
            NotifyStatus.PausedNotified,
            NotifyStatus.PreProcessing,
            NotifyStatus.Processing,
            NotifyStatus.ProcessingNotified,
        };
        private static readonly NotifyStatus[] _doneMap =
        {
            NotifyStatus.None,
            NotifyStatus.Notified,
            NotifyStatus.Paused,
            NotifyStatus.PausedNotified,
            NotifyStatus.PreProcessing,
            NotifyStatus.None,
            NotifyStatus.Notified,
        };

        public void Process(Action<bool, Action> process)
        {
            var p = ProcessEx();
            var dispose = default(Action);
            process(p.Item1, () => dispose = p.Item2());
            dispose();
        }

        public IDisposable Process()
        {
            var p = ProcessEx();
            var dispose = p.Item2();
            return Disposable.Create(dispose);
        }

        private int _pauseThread;
        private int _processingThread;
        private int _status;
        public Action OnNotify;
    }
}
