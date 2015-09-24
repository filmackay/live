using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<TResult> SelectValue<TSource, TSourceIDelta, TResult>(this ILiveCollection<TSource, TSourceIDelta> source, Func<TSource, TResult> selector)
            where TSourceIDelta : ICollectionDelta<TSource>
            where TResult : struct
        {
            LiveObserver<ICollectionState<TSource, TSourceIDelta>> observer = null;

            return LiveCollectionObservable<TResult>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    var state = observer.GetState(stateLock);
                    return state.Extract<TSource, TSourceIDelta, TResult>(true,
                        (sourceInner, sourceDelta) => sourceDelta.ToCollectionDelta<TSource,TResult>(inner => inner.Select(selector)),
                        sourceInner => sourceInner.Select(selector));
                },
                () => observer.Dispose());
        }

        public static ILiveSet<TResult> SelectUniqueValue<TSource, TSourceIDelta, TResult>(this ILiveCollection<TSource, TSourceIDelta> source, Func<TSource, TResult> selector)
            where TSourceIDelta : ICollectionDelta<TSource>
            where TResult : struct
        {
            LiveObserver<ICollectionState<TSource, TSourceIDelta>> observer = null;

            return LiveSetObservable<TResult>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    var state = observer.GetState(stateLock);
                    return state.Extract<TSource, TSourceIDelta, TResult>(true,
                        (sourceInner, sourceDelta) => sourceDelta.ToSetDelta(inner => inner.Select(selector)),
                        sourceInner => sourceInner.Select(selector));
                },
                () => observer.Dispose());
        }

        public static ILiveCollection<TResult> SelectStatic<TSource, TSourceIDelta, TResult>(this ILiveCollection<TSource, TSourceIDelta> source, Func<TSource, TResult> selector)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            var cache = new CollectionStateCache<KeyValuePair<TSource, TResult>, ICollection<KeyValuePair<TSource, TResult>>, ICollectionDelta<KeyValuePair<TSource, TResult>>>(new MultiMap<TSource, TResult>());
            var innerCache = cache.Cache as MultiMap<TSource, TResult>;
            LiveObserver<ICollectionState<TSource, TSourceIDelta>> observer = null;

            return LiveCollectionObservable<TResult>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        // get state
                        using (var state = observer.GetState())
                        {
                            // convert delta
                            var delta = state.Delta.ToCollectionDelta
                                (inserts => inserts.Select(s => KeyValuePair.Create(s, selector(s))),
                                 deletes => deletes.Select(s => KeyValuePair.Create(s, innerCache[s])));

                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner == null ? null : state.Inner.Select(t => KeyValuePair.Create(t, selector(t))),
                                delta,
                                state.LastUpdated,
                                true);
                        }
                    }
                    stateLock.DowngradeToReader();

                    // translate state
                    return
                        cache
                            .Extract(
                                sourceDelta => sourceDelta.ToCollectionDelta(items => items.Select(kv => kv.Value)),
                                inner => inner.Select(kv => kv.Value),
                                stateLock);
                },
                () => observer.Dispose());
        }

        public static ILiveCollection<TResult> Select<T, TIDelta, TResult>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<TResult>> selector)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }
    }
}
