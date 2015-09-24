using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveList<KeyValuePair<TKey, TValue>> Sort<TKey, TValue>(this ILiveDictionary<TKey, TValue> source)
        {
            IDisposable subscription = null;
            var comparer = new KeyValuePairComparer<TKey,TValue>();
            var cache = new CollectionStateCache<KeyValuePair<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IListDelta<KeyValuePair<TKey, TValue>>>(new List<KeyValuePair<TKey, TValue>>());
            var cacheList = cache.Cache as List<KeyValuePair<TKey, TValue>>;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> observer = null;

            return LiveListObservable<KeyValuePair<TKey, TValue>>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        // get change
                        using (var state = observer.GetState())
                        {
                            // convert delta
                            ListDelta<KeyValuePair<TKey, TValue>> delta = null;
                            if (state.Delta.HasChange())
                            {
                                // remove by index
                                delta = new ListDelta<KeyValuePair<TKey, TValue>>();
                                foreach (var remove in state.Delta.Deletes)
                                {
                                    // find item that matches key
                                    var index = cacheList.BinarySearch(remove, comparer);
                                    Debug.Assert(index >= 0);
                                    if (index > 0)
                                    {
                                        delta.Delete(index, new[] {remove});
                                        cache.Cache.RemoveAt(index);
                                    }
                                }

                                // insertion sort by index
                                foreach (var add in state.Delta.Inserts)
                                {
                                    var index = cacheList.BinarySearch(add, comparer);
                                    if (index < 0)
                                        index = ~index;
                                    delta.Insert(index, new[] { add });
                                    cache.Cache.Insert(index, add);
                                }
                            }

                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner == null ? null : state.Inner.OrderBy(i => i, comparer),
                                delta,
                                state.LastUpdated,
                                false);
                        }
                    }
                    stateLock.DowngradeToReader();
                    return cache.Copy(stateLock);
                },
                () => observer.Dispose());
        }
    }
}
