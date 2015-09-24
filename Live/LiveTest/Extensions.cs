using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vertigo.Live.Test
{
    public static partial class Extensions
    {
        public static StateStatus Status<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source.States().Select(s => s.Status).First();
        }

        public static void Check<T>(this IObservable<T> source, Action action, Action<IEnumerable<T>> checkResults)
        {
            var results = new List<T>();

            using (source.Subscribe(i => results.Add(i)))
            using (Publish.Transaction(true))
                action();

            checkResults(results);

            results
                .OfType<IDisposable>()
                .ForEach(s => s.Dispose());

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void Check1<TIState>(this ILiveObservable<TIState> source, Action action, Action<IEnumerable<TIState>> checkResults) where TIState : class, IState
        {
            var results = new List<TIState>();

            var observer = source.CreateObserver(() => { });
            using (source.Subscribe(observer))
            {
                results.Add(observer.GetState());

                using (Publish.Transaction(true))
                    action();

                results.Add(observer.GetState());
            }

            checkResults(results);
            results.OfType<IDisposable>().ForEach(state => state.Dispose());

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void Verify<TKey, TElement>(this KeyValuePair<TKey, ILiveCollection<TElement>> kv, TKey key, IEnumerable<TElement> elements)
        {
            Assert.AreEqual(key, kv.Key);
            Assert.IsTrue(kv.Value.ToArray().UnorderedEqual(elements));
        }

        public static void Verify<TKey, TElement>(this IEnumerable<KeyValuePair<TKey, ILiveCollection<TElement>>> kvs, IEnumerable<KeyValuePair<TKey, TElement[]>> expected)
        {
            kvs.Zip(expected, (Actual, Expected) => new { Actual, Expected })
               .ForEach(i => i.Actual.Verify(i.Expected.Key, i.Expected.Value));
        }
    }
}