using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveDictionary<TKey, TValue> SelectDictionaryStatic<TKey, TValue>(this ILiveSet<TKey> source, Func<TKey, TValue> selector)
        {
            var cache = new CollectionStateCache<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>, IDictionaryDelta<TKey, TValue>>(new Dictionary<TKey, TValue>());
            LiveObserver<ICollectionState<TKey, ISetDelta<TKey>>> observer = null;

            return LiveDictionaryObservable<TKey, TValue>.Create(
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
                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner.Select(key => KeyValuePair.Create(key, selector(key))),
                                state.Delta.ToDictionaryDelta(
                                    addItems => addItems.Select(key => KeyValuePair.Create(key, selector(key))),
                                    removeItems => removeItems.Select(key => KeyValuePair.Create(key, cache.Cache[key]))),
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

        public static ILiveDictionary<TKey, TValue> SelectDictionary<TKey, TValue>(this ILiveSet<TKey> source, Func<TKey, ILiveValue<TValue>> selector)
        {
            return
                source
                    .SelectDictionaryStatic(selector)
                    .Unwrap();
        }
    }
}
