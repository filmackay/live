using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveList<T> ToLiveList<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            // handle LiveList
            if (source is ILiveList<T>)
                return source as ILiveList<T>;

            // generic collection
            {
                var cache = new CollectionStateCache<T, IList<T>, IListDelta<T>>(new VirtualList<T>(false));
                var cacheList = cache.Cache as VirtualList<T>;
                IDisposable subscription = null;
                LiveObserver<ICollectionState<T, TIDelta>> observer = null;

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
                            // get change
                            using (var state = observer.GetState())
                            {
                                // convert delta
                                ListDelta<T> delta = null;
                                if (state.Delta.HasChange())
                                {
                                    // remove by index
                                    delta = new ListDelta<T>();
                                    foreach (var remove in state.Delta.Deletes)
                                    {
                                        var index = cacheList.IndexOf(remove);
                                        delta.Delete(index, new[] {remove});
                                        cache.Cache.RemoveAt(index);
                                    }

                                    // append
                                    foreach (var add in state.Delta.Inserts)
                                    {
                                        var index = cacheList.Count;
                                        delta.Insert(index, new[] {add});
                                        cache.Cache.Insert(index, add);
                                    }
                                }

                                // apply source state to cache
                                cache.AddState(state.Status,
                                                state.Inner,
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
}