using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;


namespace Vertigo.Live
{
    public static class LiveScheduler
    {
        public static IDisposable Schedule(this IScheduler scheduler, ILiveValue<TimeSpan> interval, Action action)
        {
            var startTime = HiResTimer.Now();
            var gate = new object();
            var hasValue = false;
            var cancelable = new SerialDisposable();
            var id = 0UL;
            var observer =
                interval.CreateObserver(
                    o => Publish.OnConsume(
                        () =>
                        {
                            // get new interval
                            var state = o.GetState();

                            // work out interval from here
                            var timeSpan = TimeSpan.Zero;
                            if (state.Status.IsConnected())
                            {
                                timeSpan = state.NewValue - HiResTimer.ToTimeSpan(HiResTimer.Now() - startTime);
                                if (timeSpan < TimeSpan.Zero)
                                    timeSpan = TimeSpan.Zero;
                            }

                            // adapted from Observable.Throttle()
                            ulong currentid;
                            lock (gate)
                            {
                                hasValue = true;
                                id += 1UL;
                                currentid = id;
                            }
                            var timer = new SingleAssignmentDisposable();
                            cancelable.Disposable = timer;
                            timer.Disposable =
                                scheduler.Schedule(
                                    timeSpan,
                                    () =>
                                    {
                                        lock (gate)
                                        {
                                            if (hasValue && (id == currentid))
                                                action();
                                            hasValue = false;
                                        }
                                    });
                        })
                    );
            interval.Subscribe(observer);

            return new CompositeDisposable(new IDisposable[] { observer, cancelable });
        }

        /*
        private static IDisposable InvokeRec2<TState>(IScheduler scheduler, Pair<TState, Action<TState, Action<TState, TimeSpan>>> pair)
        {
            var group = new CompositeDisposable(2);
            var gate = new object();
            var first = pair.First;
            var action = pair.Second;
            Action<TState> recursiveAction = null;
            recursiveAction = delegate (TState state1) {
                action(state1, delegate (TState state2, TimeSpan dueTime1) {
                    var isAdded = false;
                    var isDone = false;
                    IDisposable d = null;
                    d = scheduler.Schedule(state2, dueTime1,
                            (scheduler1, state3) =>
                            {
                                lock (gate)
                                {
                                    if (isAdded)
                                    {
                                        group.Remove(d);
                                    }
                                    else
                                    {
                                        isDone = true;
                                    }
                                }
                                recursiveAction(state3);
                                return Disposable.Empty;
                            });
                    lock (gate)
                    {
                        if (!isDone)
                        {
                            group.Add(d);
                            isAdded = true;
                        }
                    }
                });
            };
            recursiveAction(first);
            return group;
        }

        public static IDisposable Schedule<TState>(this IScheduler scheduler, TState state, ILiveValue<TimeSpan> dueTime, Action<TState, Action<TState, TimeSpan>> action)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");
            var pair = new Pair<TState, Action<TState, Action<TState, TimeSpan>>>
            {
                First = state,
                Second = action
            };
            return scheduler.Schedule(pair, dueTime.Value, InvokeRec2);
        }

        public static IDisposable Schedule(this IScheduler scheduler, ILiveValue<TimeSpan> dueTime, Action<Action<TimeSpan>> action)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");
            return
                scheduler.Schedule(
                    action,
                    dueTime,
                    (_action, self) => _action(dt => self(_action, dt)));
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        private struct Pair<T1, T2>
        {
            public T1 First;
            public T2 Second;
        }
        */
    }
}
