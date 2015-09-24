using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveList<TValue> Sort<TKey, TValue>(this ILiveCollection<KeyValuePair<TKey, TValue>> source)
        {
            var comparer = new KeyValuePairComparer<TKey,TValue>();
            var valueEqualityComparer = EqualityComparer<TValue>.Default;
            var cache = new CollectionStateCache<KeyValuePair<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IListDelta<KeyValuePair<TKey, TValue>>>(new List<KeyValuePair<TKey, TValue>>());
            var cacheList = cache.Cache as List<KeyValuePair<TKey, TValue>>;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, ICollectionDelta<KeyValuePair<TKey, TValue>>>> observer = null;
            IDisposable subscription = null;

            var indexOfItem = new Func<KeyValuePair<TKey, TValue>, int>(item =>
            {
                // find item that matches key
                var index = cacheList.BinarySearch(item, comparer);
                if (index < 0)
                    return -1;
                if (valueEqualityComparer.Equals(cacheList[index].Value, item.Value))
                    return index;

                // move back to first match
                while (index > 0 && comparer.Compare(item, cacheList[index - 1]) == 0)
                {
                    index--;
                    if (valueEqualityComparer.Equals(cacheList[index].Value, item.Value))
                        return index;
                }

                // move forward to last match
                while (index < cacheList.Count && comparer.Compare(item, cacheList[index + 1]) == 0)
                {
                    index++;
                    if (valueEqualityComparer.Equals(cacheList[index].Value, item.Value))
                        return index;
                }

                return -1;
            });

            return LiveListObservable<TValue>.Create(
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
                                if (state.Delta.Deletes != null)
                                    foreach (var remove in state.Delta.Deletes)
                                    {
                                        // find item that matches key
                                        var index = indexOfItem(remove);
                                        Debug.Assert(index >= 0);
                                        if (index != -1)
                                        {
                                            delta.Delete(index, new[] {remove});
                                            cache.Cache.RemoveAt(index);
                                        }
                                    }

                                // insertion sort by index
                                if (state.Delta.Inserts != null)
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
                    return cache.Extract
                        (delta => delta.ToListDelta(kv => kv.Value),
                         inner => inner.Select(kv => kv.Value),
                         stateLock);
                },
                () => observer.Dispose());
        }
    }
}
