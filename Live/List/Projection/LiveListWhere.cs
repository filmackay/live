using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Filter<T>(this ILiveList<Tuple<bool, T>> source)
        {
            IDisposable subscription = null;
            LiveObserver<ICollectionState<Tuple<bool, T>, IListDelta<Tuple<bool, T>>>> observer = null;
            var cache = new CollectionStateCache<T, IList<T>, IListDelta<T>>(new VirtualList<T>(false));
            var cacheList = cache.Cache as VirtualList<T>;

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        using (var state = observer.GetState())
                        {
                            // convert to dense delta
                            var delta = state.Delta.ToListDelta<Tuple<bool, T>, T>((newDelta, changes) =>
                            {
                                foreach (var change in changes)
                                {
                                    var deletes = change.Data
                                        .DeleteItems
                                        .Where(kv => kv.Item1)
                                        .Select(kv => kv.Item2)
                                        .ToArray();
                                    if (deletes.Length > 0)
                                    {
                                        var denseIndex = cacheList.DenseIndexOfIndex(change.Index);
                                        newDelta.Delete(denseIndex, deletes);
                                        cache.Cache.RemoveRange(change.Index, deletes.Length);
                                    }

                                    var inserts = change.Data
                                        .InsertItems
                                        .Where(kv => kv.Item1)
                                        .Select(kv => kv.Item2)
                                        .ToArray();
                                    if (inserts.Length > 0)
                                    {
                                        var denseIndex = cacheList.DenseIndexOfIndex(change.Index);
                                        newDelta.Insert(denseIndex, inserts);
                                        cache.Cache.InsertRange(change.Index, inserts);
                                    }
                                }
                            });

                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner.Where(kv => kv.Item1).Select(kv => kv.Item2),
                                delta,
                                state.LastUpdated,
                                false);
                        }
                    }
                    stateLock.DowngradeToReader();

                    // translate cache into dense version
                    return cache.Extract
                        (delta => delta,
                         inner => (inner as VirtualList<T>).Dense,
                         stateLock);                    
                },
                () => observer.Dispose());
        }

        public static ILiveList<T> Where<T>(this ILiveList<T> source, Func<T, ILiveValue<bool>> filter)
        {
            return source
                .Select(v => LiveTuple.Create(filter(v), v))
                .Filter();
        }

        public static ILiveList<T> Where<T>(this ILiveList<T> source, Func<T, bool> filter)
        {
            return source
                .SelectStatic(v => Tuple.Create(filter(v), v))
                .Filter();
        }
    }
}
