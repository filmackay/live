using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<KeyValuePair<TKey, Tuple<TOuter, TInner>>> Join<TOuter, TOuterIDelta, TInner, TInnerIDelta, TKey>(this ILiveCollection<KeyValuePair<TKey, TOuter>, TOuterIDelta> outer, ILiveCollection<KeyValuePair<TKey, TInner>, TInnerIDelta> inner)
            where TOuterIDelta : class, ICollectionDelta<KeyValuePair<TKey, TOuter>>
            where TInnerIDelta : class, ICollectionDelta<KeyValuePair<TKey, TInner>>
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TOuter>, TOuterIDelta>> outerObserver = null;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TInner>, TInnerIDelta>> innerObserver = null;

            return LiveCollectionObservable<KeyValuePair<TKey, Tuple<TOuter, TInner>>>.Create(
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
                    CollectionDelta<KeyValuePair<TKey, Tuple<TOuter, TInner>>> delta;
                    if (outerState.Delta == null && innerState.Delta == null)
                        delta = null;
                    else
                    {
                        delta = new CollectionDelta<KeyValuePair<TKey, Tuple<TOuter, TInner>>>();

                        // get past and present
                        var outerAll = outerState.Inner;
                        if (outerState.Delta != null)
                            outerAll = outerAll.Concat(outerState.Delta.Deletes);
                        var innerAll = innerState.Inner;
                        if (innerState.Delta != null)
                            innerAll = innerAll.Concat(innerState.Delta.Deletes);

                        // apply inserts
                        if (outerState.Delta != null)
                            delta.Insert(-1, outerState.Delta.Inserts.Join(innerAll));
                        if (innerState.Delta != null)
                            delta.Insert(-1, outerAll.Join(innerState.Delta.Inserts));
                        if (outerState.Delta != null && innerState.Delta != null)
                            delta.Delete(-1, outerState.Delta.Inserts.Join(innerState.Delta.Inserts));

                        // apply deletes
                        if (outerState.Delta != null)
                            delta.Delete(-1, outerState.Delta.Deletes.Join(innerAll));
                        if (innerState.Delta != null)
                            delta.Delete(-1, outerAll.Join(innerState.Delta.Deletes));
                        if (outerState.Delta != null && innerState.Delta != null)
                            delta.Insert(-1, outerState.Delta.Deletes.Join(innerState.Delta.Deletes));
                    }

                    // detach source locks
                    innerState.InnerSourceLock();
                    outerState.InnerSourceLock();

                    // prepare result state
                    var ret = new CollectionState<KeyValuePair<TKey, Tuple<TOuter, TInner>>, ICollectionDelta<KeyValuePair<TKey, Tuple<TOuter, TInner>>>, ICollection<KeyValuePair<TKey, Tuple<TOuter, TInner>>>>();
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

        public static ILiveCollection<KeyValuePair<TKey, Tuple<TOuter, TInner>>> Join<TOuter, TOuterIDelta, TInner, TInnerIDelta, TKey>
            (this ILiveCollection<TOuter, TOuterIDelta> outer,
            ILiveCollection<TInner, TInnerIDelta> inner,
            Func<TOuter, ILiveValue<TKey>> outerKeySelector,
            Func<TInner, ILiveValue<TKey>> innerKeySelector)
            where TOuterIDelta : class, ICollectionDelta<TOuter>
            where TInnerIDelta : class, ICollectionDelta<TInner>
        {
            return outer
                .Select(o => LiveKeyValuePair.Create(outerKeySelector(o), o))
                .Join(inner.Select(i => LiveKeyValuePair.Create(innerKeySelector(i), i)));
        }

        public static ILiveCollection<TResult> Join<TOuter, TOuterIDelta, TInner, TInnerIDelta, TKey, TResult>
            (this ILiveCollection<TOuter, TOuterIDelta> outer,
            ILiveCollection<TInner, TInnerIDelta> inner,
            Func<TOuter, LiveValue<TKey>> outerKeySelector,
            Func<TInner, LiveValue<TKey>> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TOuterIDelta : class, ICollectionDelta<TOuter>
            where TInnerIDelta : class, ICollectionDelta<TInner>
        {
            return outer
                .Join(inner, outerKeySelector, innerKeySelector)
                .SelectStatic(kv => resultSelector(kv.Value.Item1, kv.Value.Item2));
        }
    }
}
