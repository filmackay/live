using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveDictionary<TKey, TValue> AsLiveDictionary<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<TKey, TValue>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, TValue>>
        {
            var cache = new CollectionStateCache<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>, IDictionaryDelta<TKey, TValue>>(new Dictionary<TKey, TValue>());
            IDisposable subscription = null;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, TIDelta>> observer = null;

            return LiveDictionaryObservable<TKey, TValue>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
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
                            var delta = state.Delta.ToDictionaryDelta(items => items);

                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner,
                                delta,
                                state.LastUpdated,
                                true);
                        }
                    }
                    stateLock.DowngradeToReader();

                    // translate state
                    return cache.Copy(stateLock);
                },
                () => observer.Dispose());
        }
    }
}
