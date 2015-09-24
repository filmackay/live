using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public static class HiResTimer
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        static private readonly long _frequency;

        // Constructor

        static HiResTimer()
        {
            QueryPerformanceFrequency(out _frequency);
        }

        public static long Now()
        {
            long ret;
            QueryPerformanceCounter(out ret);
            return ret;
        }

        public static long FromTimeSpan(TimeSpan time)
        {
            return (long)(time.TotalSeconds * _frequency);
        }

        public static long FromTicks(long ticks)
        {
            return (long)(ticks / 10000000.0 * _frequency);
        }

        public static TimeSpan ToTimeSpan(long counter)
        {
            return TimeSpan.FromTicks(ToTicks(counter));
        }

        public static double ToMilliseconds(long counter)
        {
            return (counter * 1000.0 / _frequency);
        }

        public static double ToMicroseconds(long counter)
        {
            return (counter * 1000000.0 / _frequency);
        }

        public static long ToTicks(long counter)
        {
            return (long)(counter * 10000000.0 / _frequency);
        }

        public static double ToSeconds(long counter)
        {
            return ((double)counter / _frequency);
        }

        public static TimeSpan Duration(long begin, long end)
        {
            return ToTimeSpan(end - begin);
        }
    }

    public enum WaitStrategy
    {
        Spin,
        Yield,
        Sleep,
    };

    public class HiResScheduler : IScheduler
    {
        public static HiResScheduler Instance = new HiResScheduler();
        private long _nextTime;
        private readonly List<Tuple<long, Action>> _actions = new List<Tuple<long, Action>>();
        private static readonly long _smallTime = HiResTimer.FromTimeSpan(TimeSpan.FromSeconds(0.001));
        public WaitStrategy WaitStrategy = WaitStrategy.Sleep;
        public bool BypassImmediate;
        private bool _running;

        public IDisposable Start()
        {
            lock (this)
            {
                if (_running)
                    return null;

                var stopping = false;
                var thread = new Thread(_ =>
                {
                    _running = true;
                    while (!stopping)
                    {
                        // anything to process?
                        if (_nextTime > 0)
                        {
                            var now = HiResTimer.Now();
                            var remaining = _nextTime - now;
                            if (remaining <= 0)
                            {
                                // something to process
                                var actions = new List<Action>();
                                lock (_actions)
                                {
                                    _nextTime = 0;
                                    for (var i = 0; i < _actions.Count; i++)
                                    {
                                        var a = _actions[i];

                                        // item ready to go?
                                        if (a.Item1 <= now)
                                        {
                                            _actions.RemoveAt(i);
                                            actions.Add(a.Item2);
                                            i--;
                                        }
                                        else
                                        {
                                            // update _nextTime
                                            if (_nextTime == 0 || a.Item1 < _nextTime)
                                                _nextTime = a.Item1;
                                        }
                                    }

                                    // recalculate remaining
                                    remaining = _nextTime == 0 ? long.MaxValue : _nextTime - now;
                                }

                                actions.ForEach();
                            }
                            else if (remaining < _smallTime)
                            {
                                // avoid waiting
                                continue;
                            }
                        }

                        // wait
                        switch (WaitStrategy)
                        {
                            case WaitStrategy.Spin:
                                break;
                            case WaitStrategy.Yield:
                                Thread.Yield();
                                break;
                            case WaitStrategy.Sleep:
                                Thread.Sleep(1);
                                break;
                        }
                    }

                    _running = false;
                }) { IsBackground = true };
                thread.Start();

                return Disposable.Create(() =>
                {
                    stopping = true;
                    thread.Join();
                });
            }
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            return action(this, state);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return Schedule(HiResTimer.Now() + HiResTimer.FromTimeSpan(dueTime), () => action(this, state));
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return Schedule(dueTime.Ticks, () => action(this, state));
        }

        public IDisposable Schedule(long dueTime, Action action)
        {
            if (!_running)
                Start();

            // immediate?
            if (BypassImmediate)
            {
                var now = HiResTimer.Now();
                var remaining = dueTime - now;
                if (remaining <= 0)
                {
                    // perform synchronously
                    action();
                    return null;
                }
            }

            // send to background thread
            var tuple = Tuple.Create(dueTime, action);
            lock (_actions)
            {
                _actions.Add(Tuple.Create(dueTime, action));
                if (_nextTime == 0 || dueTime < _nextTime)
                    _nextTime = dueTime;
            }

            // cancel
            return Disposable.Create(() =>
            {
                lock (_actions)
                    _actions.Remove(tuple);
            });
        }

        public DateTimeOffset Now
        {
            get { return new DateTimeOffset(HiResTimer.ToTicks(HiResTimer.Now()), TimeSpan.Zero); }
        }
    }
}
