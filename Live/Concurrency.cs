using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public static class Concurrency
    {
        public static int Atomic(ref int result, Func<int, int> func)
        {
            int oldValue, newValue;
            do
            {
                oldValue = result;
                newValue = func(oldValue);
            } while (Interlocked.CompareExchange(ref result, newValue, oldValue) != oldValue);

            return oldValue;
        }

        public static Tuple<long, long> Atomic(ref long result, Func<long, long> func)
        {
            long oldValue, newValue;
            do
            {
                oldValue = result;
                newValue = func(oldValue);
            } while (Interlocked.CompareExchange(ref result, newValue, oldValue) != oldValue);

            return new Tuple<long, long>(oldValue, newValue);
        }

        public static Tuple<T, T> Atomic<T>(ref T result, Func<T, T> func)
            where T : class
        {
            T oldValue, newValue;
            do
            {
                oldValue = result;
                newValue = func(oldValue);
            } while (Interlocked.CompareExchange(ref result, newValue, oldValue) != oldValue);

            return new Tuple<T, T>(oldValue, newValue);
        }
    }

    public interface IGovernor
    {
        Task<Task<T>> Govern<T>(Func<Task<T>> func);
        Task<Task> Govern(Func<Task> func);
    }

    public class ConcurrencyGovernor : IGovernor
    {
        private readonly SemaphoreSlim _semaphore;

        public ConcurrencyGovernor(int maxConcurrency)
        {
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        public int Current
        {
            get { return _semaphore.CurrentCount; }
        }

        public Task<Task> Govern(Func<Task> func)
        {
            return Task.Factory.StartNew(() =>
            {
                _semaphore.Wait();
                return func()
                    .ContinueWith(t =>
                    {
                        _semaphore.Release();
                    });
            });
        }

        public Task<Task<T>> Govern<T>(Func<Task<T>> func)
        {
            return Task.Factory.StartNew(() =>
            {
                _semaphore.Wait();
                return func()
                    .ContinueWith(t =>
                    {
                        _semaphore.Release();
                        return t.Result;
                    });
            });
        }
    }

    //public class RateGovernor : IGovernor
    //{
    //    private readonly long _min;
    //    private long _next;

    //    public RateGovernor(double maxPerSecond)
    //    {
    //        _min = HiResTimer.FromTimeSpan(TimeSpan.FromSeconds(1.0 / maxPerSecond));
    //    }

    //    public Task<Task> Govern(Func<Task> func)
    //    {
    //        // work out next time slot
    //        var now = HiResTimer.Now();
    //        var waitUntil = Concurrency.Atomic(ref _next, oldNext => Math.Max(oldNext, now) + _min).Item1;
    //        var wait = waitUntil - now;

    //        // run func
    //        return wait > 0
    //            ? TaskEx
    //                .Delay(HiResTimer.ToTimeSpan(wait))
    //                .ContinueWith(t => func())
    //            : TaskEx.FromResult(func());
    //    }

    //    public Task<Task<T>> Govern<T>(Func<Task<T>> func)
    //    {
    //        // work out next time slot
    //        var now = HiResTimer.Now();
    //        var waitUntil = Concurrency.Atomic(ref _next, oldNext => Math.Max(oldNext, now) + _min).Item1;
    //        var wait = waitUntil - now;

    //        // run func
    //        return wait > 0
    //            ? TaskEx
    //                .Delay(HiResTimer.ToTimeSpan(wait))
    //                .ContinueWith(t => func())
    //            : TaskEx.FromResult(func());
    //    }
    //}

    public static partial class Extensions
    {
        public static int LeastConcurrent(this IList<ConcurrencyGovernor> throttles)
        {
            if (throttles.Count == 0)
                return -1;

            var index = 0;
            var min = throttles[index].Current;
            if (min == 0)
                return index;
            for (var i = 0; i < throttles.Count; i++)
            {
                if (min < throttles[i].Current)
                {
                    index = i;
                    min = throttles[index].Current;
                    if (min == 0)
                        return index;
                }
            }
            return index;
        }

        public static IObservable<TResult> ConcurrencyGovernor<TSource, TResult>(this IObservable<TSource> source, int concurrecy, Func<TSource, Task<TResult>> func)
        {
            var governor = new ConcurrencyGovernor(concurrecy);
            return source.SelectMany(i => governor.Govern(() => func(i)).Result.ToObservable());
        }
    }
}