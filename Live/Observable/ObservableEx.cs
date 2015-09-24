using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Vertigo.Live
{
    public static class ObservableEx
    {
        public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, IObservable<TimeSpan> dueTime, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.ThreadPool;
            return dueTime
                .Select(time => source.Delay(time, scheduler))
                .Switch();
        }

        public static IObservable<TSource> DelayBypassZero<TSource>(this IObservable<TSource> source, IObservable<TimeSpan> dueTime, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.ThreadPool;
            return dueTime
                .Select(time => time.IsZero() ? source : source.Delay(time, scheduler))
                .Switch();
        }

        public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, IObservable<TimeSpan> dueTime, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.ThreadPool;
            return dueTime
                .Select(time => source.Throttle(time, scheduler))
                .Switch();
        }

        public static IObservable<TSource> ThrottleBypassZero<TSource>(this IObservable<TSource> source, IObservable<TimeSpan> dueTime, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.ThreadPool;
            return dueTime
                .Select(time => time <= TimeSpan.Zero ? source : source.Throttle(time, scheduler))
                .Switch();
        }

        //public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, ILiveValue<TimeSpan> dueTime, IScheduler scheduler = null)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException("source");
        //    scheduler = scheduler ?? Scheduler.ThreadPool;
        //    var dueTimeObserver = dueTime.Subscribe(o => { });
        //    dueTimeObserver.GetState();
        //    return Observable.Create<TSource>(observer =>
        //    {
        //        var id = _id++;
        //        var gate = new object();
        //        var q = new Queue<Timestamped<Notification<TSource>>>();
        //        var active = false;
        //        var running = false;
        //        var cancelable = new MutableDisposable(AssignmentBehavior.ReplaceAndDisposePrevious);
        //        Exception exception = null;
        //        var disposable = source.Materialize().Timestamp(scheduler).Subscribe(notification =>
        //        {
        //            Action<Action<TimeSpan>> action = null;
        //            bool flag;
        //            lock (gate)
        //            {
        //                if (notification.Value.Kind == NotificationKind.OnError)
        //                {
        //                    q.Clear();
        //                    q.Enqueue(notification);
        //                    exception = notification.Value.Exception;
        //                    flag = !running;
        //                }
        //                else
        //                {
        //                    q.Enqueue(new Timestamped<Notification<TSource>>(notification.Value, notification.Timestamp.Add(dueTimeObserver.GetState().NewValue)));
        //                    flag = !active;
        //                    active = true;
        //                }
        //            }
        //            if (flag)
        //            {
        //                if (exception != null)
        //                {
        //                    observer.OnError(exception);
        //                }
        //                else
        //                {
        //                    var disposable2 = new MutableDisposable(AssignmentBehavior.SingleAssignment);
        //                    cancelable.Disposable = disposable2;
        //                    if (action == null)
        //                    {
        //                        action = (Action<TimeSpan> self) =>
        //                        {
        //                            Notification<TSource> notification2;
        //                            lock (gate)
        //                            {
        //                                if (exception != null)
        //                                {
        //                                    return;
        //                                }
        //                                running = true;
        //                            }
        //                            do
        //                            {
        //                                notification2 = null;
        //                                lock (gate)
        //                                {
        //                                    if ((q.Count > 0) && (q.Peek().Timestamp.CompareTo(scheduler.Now) <= 0))
        //                                    {
        //                                        notification2 = q.Dequeue().Value;
        //                                    }
        //                                }
        //                                if (notification2 != null)
        //                                {
        //                                    notification2.Accept(observer);
        //                                }
        //                            }
        //                            while (notification2 != null);
        //                            var flag3 = false;
        //                            var zero = TimeSpan.Zero;
        //                            Exception error = null;
        //                            lock (gate)
        //                            {
        //                                if (q.Count > 0)
        //                                {
        //                                    flag3 = true;
        //                                    zero = TimeSpan.FromTicks(Math.Max(0L, q.Peek().Timestamp.Subtract(scheduler.Now).Ticks));
        //                                }
        //                                else
        //                                {
        //                                    active = false;
        //                                }
        //                                error = exception;
        //                                running = false;
        //                            }
        //                            if (error != null)
        //                            {
        //                                observer.OnError(error);
        //                            }
        //                            else if (flag3)
        //                            {
        //                                LogManager.Entry(LogType.Debug, "Delay", string.Format(" self #{0} {1}ms", id, zero.TotalMilliseconds));
        //                                self(zero);
        //                            }
        //                        };
        //                    }
        //                    var dueTimeValue = dueTimeObserver.GetState().NewValue;
        //                    LogManager.Entry(LogType.Debug, "Delay", string.Format("#{0} {1}ms", id, dueTimeValue.TotalMilliseconds));
        //                    disposable2.Disposable = scheduler.Schedule(dueTimeValue, action);
        //                }
        //            }
        //        });
        //        return new CompositeDisposable(new [] { disposable, cancelable });
        //    });
        //}

        //private static int _id;
    }
}
