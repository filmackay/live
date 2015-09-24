using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveDictionary<TKey, Tuple<TOuter, TInner>> Join<TOuter, TInner, TKey>(this ILiveDictionary<TKey, TOuter> outer, ILiveDictionary<TKey, TInner> inner)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TOuter>, IDictionaryDelta<TKey, TOuter>>> outerObserver = null;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TInner>, IDictionaryDelta<TKey, TInner>>> innerObserver = null;

            return LiveDictionaryObservable<TKey, Tuple<TOuter, TInner>>.Create(
                innerChanged =>
                {
                    outer.Subscribe(outerObserver = outer.CreateObserver(innerChanged));
                    inner.Subscribe(innerObserver = inner.CreateObserver(innerChanged));
                    return new[] { outerObserver.GetStateLock, innerObserver.GetStateLock }.MergeLockers();
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get states
                    var locks = ((CompositeDisposable)stateLock).ToArray();
                    var outerState = outerObserver.GetState(locks[0]);
                    var innerState = innerObserver.GetState(locks[1]);

                    // work out delta
                    DictionaryDelta<TKey, Tuple<TOuter, TInner>> delta = null;
                    if (outerState.Delta != null && innerState.Delta != null)
                    {
                        delta = new DictionaryDelta<TKey, Tuple<TOuter, TInner>>();

                        // get past and present
                        var outerAll = outerState.Inner.Concat(outerState.Delta.Deletes);
                        var innerAll = innerState.Inner.Concat(innerState.Delta.Deletes);

                        // apply inserts
                        delta.Insert(-1, outerState.Delta.Inserts.Join(innerAll));
                        delta.Insert(-1, outerAll.Join(innerState.Delta.Inserts));
                        delta.Delete(-1, outerState.Delta.Inserts.Join(innerState.Delta.Inserts));

                        // apply deletes
                        delta.Delete(-1, outerState.Delta.Deletes.Join(innerAll));
                        delta.Delete(-1, outerAll.Join(innerState.Delta.Deletes));
                        delta.Insert(-1, outerState.Delta.Deletes.Join(innerState.Delta.Deletes));
                    }

                    // detach source locks
                    innerState.InnerSourceLock();
                    outerState.InnerSourceLock();

                    // prepare result state
                    var ret = new CollectionState<KeyValuePair<TKey, Tuple<TOuter, TInner>>, IDictionaryDelta<TKey, Tuple<TOuter, TInner>>, IDictionary<TKey, Tuple<TOuter, TInner>>>();
                    ret.SetState(outerState.Status.And(innerState.Status),
                        delta,
                        outerState.Inner.Join(innerState.Inner),
                        Math.Max(outerState.LastUpdated, innerState.LastUpdated),
                        stateLock);
                    return ret;
                },
                () =>
                {
                    outerObserver.Dispose();
                    innerObserver.Dispose();
                });
        }

        public static ILiveDictionary<TKey, TResult> Join<TOuter, TInner, TKey, TResult>(this ILiveDictionary<TKey, TOuter> outer, ILiveDictionary<TKey, TInner> inner, Func<TOuter, TInner, TResult> selector)
        {
            return outer
                .Join(inner)
                .SelectDictionaryStatic(kv => selector(kv.Value.Item1, kv.Value.Item2));
        }
    }
}
