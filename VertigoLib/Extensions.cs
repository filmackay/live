using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Vertigo
{
    public static class AppHelpers
    {
        public static void AssemblyPaths(string[] paths)
        {
            // resolve DLL's
            AppDomain.CurrentDomain.AssemblyResolve +=
                (sender, args) =>
                {
                    //This handler is called only when the common language runtime tries to bind to the assembly and fails.

                    // look in all assembly paths
                    var index = args.Name.IndexOf(",");
                    var dllName = (index == -1 ? args.Name : args.Name.Substring(0, index)) + ".dll";
                    var path =
                        paths
                            .Select(p => p + dllName)
                            .FirstOrDefault(File.Exists);

                    if (path != null)
                        return Assembly.LoadFrom(path);

                    return null;
                };
        }
    }

    public static class Util
    {
        public static T[] GetEnums<T>()
        {
            return Enum
                .GetValues(typeof(T))
                .OfType<T>()
                .ToArray();
        }

        public static T?[] GetEnumsNull<T>()
            where T : struct
        {
            return new T?[] { null }
                .Concat(
                    Enum
                        .GetValues(typeof(T))
                        .OfType<T>()
                        .Select(e => e.ToNullable()))
                .ToArray();
        }
    }
    
    public static partial class Extensions
    {
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);
        public static TimeSpan ToUnix(this DateTime dateTime)
        {
            return dateTime - _unixEpoch;
        }

        public static DateTime FromUnix(this TimeSpan unixTime)
        {
            return _unixEpoch + unixTime;
        }

        public static IObservable<Tuple<TTag,T>> Tag<TTag,T>(this IObservable<T> source, TTag tag)
        {
            return source.Select(item => Tuple.Create(tag, item));
        }

        // uses ParallelExtensions
        //public static async Task<IDisposable> AsyncLock(this AsyncSemaphore semaphore)
        //{
        //    await semaphore.Wait();
        //    return Disposable.Create(semaphore.Release);
        //}

        public static string Value(this XAttribute attribute)
        {
            return attribute == null ? null : attribute.Value;
        }

        public static string Value(this XElement element)
        {
            return element == null ? null : element.Value;
        }

        //public static IObservable<IList<T>> SlidingWindowByCount<T>(this IObservable<T> source, int count)
        //{
        //    var enumerator = @this.GetEnumerator();
        //    while (enumerator.MoveNext())
        //    {
        //        TElement left = enumerator.Current;
        //        if (enumerator.MoveNext())
        //        {
        //            TElement right = enumerator.Current;
        //            yield return Tuple.Create(left, right);
        //        }
        //        else
        //            break;
        //    }
        //}

        public static IDisposable ToDebug(this IObservable<string> source, string prefix = null)
        {
            return source.Subscribe(line => Debug.WriteLine(prefix + line));
        }

        public static IDisposable ToConsole(this IObservable<string> source)
        {
            return source.Subscribe(Console.WriteLine);
        }

        public static IObservable<IList<T>> SlidingWindowByCount<T>(this IObservable<T> source, int count)
        {
            var window = new List<T>();
            return source
                .Select(last =>
                    {
                        window.Insert(0, last);
                        if (window.Count > count)
                        {
                            var remove = window.Count - count;
                            window.RemoveRange(window.Count - remove, remove);
                        }
                        return window.ToArray();
                    });
        }

        public static IObservable<Match> ToRegexMatches(this IObservable<string> source, string pattern)
        {
            var regex = new Regex(pattern);
            return source.SelectMany(input => regex.Matches(input).OfType<Match>());
        }

        public static IObservable<T> NotNull<T>(this IObservable<T?> source)
            where T : struct
        {
            return source.Where(n => n.HasValue).Select(n => n.Value);
        }

        public static IObservable<bool> None<T>(this IObservable<T> source, TimeSpan timeSpan)
        {
            return source
                .Select(t => false)
                .Amb(Observable.Timer(timeSpan).Select(t => true));
        }

        public static IObservable<T> TraceTo<T>(this IObservable<T> source, IObserver<string> target, string name)
        {
            return Observable
                .Create<T>(observer =>
                    {
                        target.OnNext(string.Format("#{0} {1} Subscribe", Thread.CurrentThread.ManagedThreadId, name));
                        return new CompositeDisposable(source
                            .Do(i => target.OnNext(string.Format("#{0} {1} OnNext: {2}", Thread.CurrentThread.ManagedThreadId, name, i)),
                                e => target.OnNext(string.Format("#{0} {1} OnError: {2}", Thread.CurrentThread.ManagedThreadId, name, e)),
                                () => target.OnNext(string.Format("#{0} {1} OnComplete", Thread.CurrentThread.ManagedThreadId, name)))
                            .Subscribe(observer),
                            Disposable.Create(() => target.OnNext(string.Format("#{0} {1} Dispose", Thread.CurrentThread.ManagedThreadId, name))));
                    });
        }

        public static IEnumerable<Tuple<TElement, TElement>> Pairs<TElement>(this IEnumerable<TElement> @this)
        {
            var enumerator = @this.GetEnumerator();

            while (enumerator.MoveNext())
            {
                TElement left = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    TElement right = enumerator.Current;
                    yield return Tuple.Create(left, right);
                }
                else
                    break;
            }
        }

        public static IObservable<T> PublishConnect<T>(this IObservable<T> source)
        {
            var ret = source.Publish();
            ret.Connect();
            return ret;
        }

        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source)
        {
            var ret = source.Replay();
            ret.Connect();
            return ret;
        }

        public static IObservable<T> RepeatWhen<T>(this IObservable<T> source, Func<T, bool> filter)
        {
            return Observable
                .Create<T>(observer =>
                    {
                        var d = new SerialDisposable();
                        return new CompositeDisposable(
                            Scheduler
                                .CurrentThread
                                .Schedule(self =>
                                    d.Disposable = source.Subscribe(t =>
                                        {
                                            if (filter(t))
                                                self();
                                            else
                                                observer.OnNext(t);
                                        },
                                        observer.OnError,
                                        observer.OnCompleted)
                                    ),
                            d);
                    });
        }

        public static IObservable<TSource> TakeWhileInc<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            return Observable.Create<TSource>(observer =>
            {
                var running = true;
                var d = new SingleAssignmentDisposable();
                d.Disposable = source
                    .Subscribe(x =>
                    {
                        if (running)
                        {
                            try
                            {
                                running = predicate(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                            if (running)
                            {
                                observer.OnNext(x);
                            }
                            else
                            {
                                observer.OnNext(x);
                                observer.OnCompleted();
                            }
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);

                return d;
            });
        }

        public static IEnumerable<T> GetEnums<T>(this Enum e)
        {
            var enumerator = Enum.GetValues(e.GetType()).GetEnumerator();
            while ((enumerator.MoveNext()) && (enumerator.Current != null))
            {
                yield return (T)enumerator.Current;
            }
        }

        public static IEnumerable<T> For<T>(T start, Func<T, bool> cont, Func<T, T> step)
        {
            for (var v = start; cont(v); v = step(v))
                yield return v;
        }

        public static string DelimeteredList<T>(this IEnumerable<T> list, string delimeter)
        {
            return list
                .Select(i => i == null ? null : i.ToString())
                .DelimeteredList(delimeter);
        }

        public static string DelimeteredList(this IEnumerable<string> list, string delimeter)
        {
            var ret = string.Empty;
            var first = true;
            foreach (var item in list)
            {
                if (first)
                    first = false;
                else
                    ret += delimeter;
                ret += item;
            }
            return ret;
        }

        public static bool IsZero(this DateTime date)
        {
            return date.ToBinary() == 0;
        }

        public static T? ToNullable<T>(this T val)
            where T : struct
        {
            return ToNullable(val, default(T));
        }

        public static T? ToNullable<T>(this T val, T nullValue)
            where T : struct
        {
            if (val.Equals(nullValue))
                return null;
            return val;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }

        public static void AddRange<T>(this IList<T> collection, IEnumerable<T> items)
        {
            items.ForEach(collection.Add);
        }

        public static IEnumerable<string> SplitWithPrefix(this string source, char[] delimeters)
        {
            int startIndex = 0;

            while(true)
            {
                int endIndex = source.IndexOfAny(delimeters, startIndex + 1);
                if (endIndex == -1)
                {
                    yield return source.Substring(startIndex);
                    break;
                }
                yield return source.Substring(startIndex, endIndex - startIndex);
                startIndex = endIndex;
            }
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
                dictionary.Add(item);
        }

        public static string ToString2(this TimeSpan time)
        {
            if (time.TotalDays > 1)
                return string.Format("T-{0}d", Math.Round(time.TotalDays));
            return string.Format("T{0}{1}:{2:D2}:{3:D2}", time.TotalMilliseconds < 0 ? "+" : "-", Math.Abs(time.Hours), Math.Abs(time.Minutes), Math.Abs(time.Seconds));
        }

        public static Timer StartTimer(this Action action, bool callNow, TimeSpan timeSpan)
        {
            if (callNow)
                action();
            return new Timer(o => action(), null, TimeSpan.FromMilliseconds(-1), timeSpan);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> SequentialGroupBy<TElement, TKey>(this IEnumerable<TElement> source, Func<TElement, TKey> selector)
        {
            return source.SequentialGroupBy(selector, EqualityComparer<TKey>.Default);
        }

        public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly TKey _key;
            private readonly IEnumerable<TElement> _elements;

            public Grouping(TKey key, IEnumerable<TElement> elements)
            {
                _key = key;
                _elements = elements;
            }

            public TKey Key
            {
                get { return _key; }
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                return _elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _elements.GetEnumerator();
            }
        }

        public static IEnumerable<IGrouping<TKey, TElement>> SequentialGroupBy<TElement, TKey>(this IEnumerable<TElement> source, Func<TElement, TKey> selector, IEqualityComparer<TKey> comparer)
        {
            using (var sourceIterator = source.GetEnumerator())
            {
                var first = true;
                var groupKey = default(TKey);
                List<TElement> groupList = null;
                while (sourceIterator.MoveNext())
                {
                    var current = sourceIterator.Current;
                    var key = selector(current);

                    if (first || !comparer.Equals(key, groupKey))
                    {
                        if (first)
                            first = false;
                        else
                            yield return new Grouping<TKey, TElement>(groupKey, groupList);
                        groupList = new List<TElement>();
                        groupKey = key;
                    }
                    groupList.Add(current);
                }

                yield return new Grouping<TKey, TElement>(groupKey, groupList);
            }
        }

        /*
        public static IObservable<T> OnNext<T>(this IObservable<T> observable, Action<T> action)
        {
            observable
                .Take(1)
                .Subscribe(action);
            return observable.Skip(1);
        }

        public static Task<TSource[]> BulkUp<TSource>(this IObservable<TSource> source)
        {
            return Observable.Create<TSource[]>(observer =>
                {
                    var list = new List<TSource>();
                    return source
                        .SynchronizeEx(list)
                        .Subscribe(list.Add,
                            () =>
                            {
                                observer.OnNext(list.ToArray());
                                observer.OnCompleted();
                            });
                })
                .ToTask();
        }

        public static T OnDispatcher<T>(this DispatcherObject obj, Func<T> func)
        {
            if (obj.CheckAccess())
                return func();
            var ret = default(T);
            obj.Dispatcher.Invoke(new Action(() => ret = func()));
            return ret;
        }

        public static void OnDispatcher(this DispatcherObject obj, Action action)
        {
            if (obj.CheckAccess())
                action();
            else
                obj.Dispatcher.Invoke(action);
        }*/
    }
}
