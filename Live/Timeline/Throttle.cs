using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static IObservable<TSource> SimpleThrottle<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler = null)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            scheduler = scheduler ?? Scheduler.ThreadPool;

            var timer = new SerialDisposable();
            var lastItem = default(TSource);
            var gate = new object();

            return Observable.Create<TSource>(observer =>
            {
                var subscription = source
                    .Subscribe(item =>
                        {
                            lock (gate)
                            {
                                lastItem = item;
                                if (timer.Disposable == null)
                                {
                                    // start new timer
                                    timer.Disposable = scheduler.Schedule(
                                        dueTime,
                                        () =>
                                        {
                                            TSource last;
                                            lock (gate)
                                            {
                                                last = lastItem;
                                                timer.Disposable = null;
                                            }
                                            observer.OnNext(last);
                                        });
                                }
                            }
                        },
                        exception =>
                        {
                            timer.Dispose();
                            observer.OnError(exception);
                        },
                        () =>
                        {
                            timer.Dispose();
                            observer.OnCompleted();
                        });

                return new CompositeDisposable(new[] { subscription, timer });
            });
        }

        public static IObservable<TSource> SimpleThrottle<TSource>(this IObservable<TSource> source, IObservable<TimeSpan> dueTime, IScheduler scheduler = null, bool bypassSchedulerOnZero = true)
        {
            return dueTime
                .Select(dt => bypassSchedulerOnZero && dt <= TimeSpan.Zero
                    ? source
                    : source.SimpleThrottle(dt, scheduler))
                .Switch();
        }
    }
}
