using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        // delays the notification of changes to subscribers until a period after it is notified
        public static ILiveValue<T> ThrottleDelay<T>(this ILiveValue<T> source, ILiveValue<TimeSpan> interval, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.ThreadPool;
            var timer = 0L;
            var observer = default(LiveObserver<IValueState<T>>);
            var intervalObserver = interval.Subscribe(o => { });
            intervalObserver.GetState();
            
            return LiveValueObservable<T>.Create(
                innerChanged =>
                    source
                        .Subscribe(
                            observer = source.CreateObserver(() =>
                                {
                                    // take snapshot of interval
                                    var dueTime = intervalObserver.GetState().NewValue;
                                    if (dueTime.IsZero())
                                        // send immediately as there is no delay
                                        innerChanged();
                                    else
                                    {
                                        var oldTimer = Interlocked.CompareExchange(ref timer, 1, 0);
                                        if (oldTimer == 0)
                                        {
                                            // schedule it for the future
                                            scheduler.Schedule(
                                                interval.Value,
                                                () =>
                                                {
                                                    timer = 0;
                                                    Publish.Transaction(() => Publish.OnPublish(innerChanged));
                                                });
                                        }
                                    }
                                })
                            ),
                () => observer.GetNotify(),
                (innerChanged, oldState) => observer.GetState(),
                () => observer.Dispose());
        }

        //// delays the notification of changes to subscribers until a period after it is notified
        //public static ILiveObservable<TIState> ThrottleDelay<TIState>(this ILiveObservable<TIState> source, ILiveValue<TimeSpan> interval, IScheduler scheduler = null)
        //    where TIState : IState
        //{
        //    scheduler = scheduler ?? Scheduler.ThreadPool;
        //    var timer = 0L;
        //    var observer = default(LiveObserver<TIState>);
        //
        //    return LiveObservable<TIState>.Create(
        //        innerChanged =>
        //            observer =
        //                source
        //                    .Subscribe(
        //                        o =>
        //                        {
        //                            var oldTimer = Interlocked.CompareExchange(ref timer, 1, 0);
        //                            if (oldTimer == 0)
        //                                scheduler
        //                                    .Schedule(
        //                                        interval.Value,
        //                                        () =>
        //                                        {
        //                                            timer = 0;
        //                                            innerChanged();
        //                                        });
        //                        }),
        //        () => observer.GetNotify(),
        //        (innerChanged, oldState) => observer.GetState(),
        //        () => observer.Dispose());
        //}
    }
}
