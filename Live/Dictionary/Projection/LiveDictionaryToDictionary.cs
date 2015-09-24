using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveDictionary<TKey, TResult> SelectDictionaryStatic<TKey, TSource, TResult>(this ILiveDictionary<TKey, TSource> source, Func<KeyValuePair<TKey, TSource>, TResult> selector)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TSource>, IDictionaryDelta<TKey, TSource>>> observer = null;
            var cache = new CollectionStateCache<KeyValuePair<TKey, TResult>, IDictionary<TKey, TResult>, IDictionaryDelta<TKey, TResult>>(new Dictionary<TKey, TResult>());

            return LiveDictionaryObservable<TKey,TResult>.Create(
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
                            var delta = state.Delta.ToDictionaryDelta
                                (inserts => inserts.Select(kv => KeyValuePair.Create(kv.Key, selector(kv))),
                                 deletes => deletes.Select(kv => KeyValuePair.Create(kv.Key, cache.Cache[kv.Key])));

                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner.Select(kv => KeyValuePair.Create(kv.Key, selector(kv))),
                                delta,
                                state.LastUpdated,
                                true);
                        }
                    }
                    stateLock.DowngradeToReader();

                    // return state copy
                    return cache.Copy(stateLock);
                },
                () => observer.Dispose());
        }

        public static ILiveDictionary<TKey, TResult> SelectDictionary<TKey, TSource, TResult>(this ILiveDictionary<TKey, TSource> source, Func<KeyValuePair<TKey, TSource>, ILiveValue<TResult>> selector)
        {
            return source
                .SelectDictionaryStatic(selector)
                .Unwrap();
        }
    }
}
