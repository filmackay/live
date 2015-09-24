using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveList<T> ToLiveList<T>(this ILiveCollection<T, ISetDelta<T>> source)
        {
            var cache = new CollectionStateCache<T, IList<T>, IListDelta<T>>(new VirtualList<T>(true));
            LiveObserver<ICollectionState<T, ISetDelta<T>>> observer = null;

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        using (var state = observer.GetState())
                        {
                            // translate delta
                            ListDelta<T> delta = null;
                            if (state.Delta.HasChange())
                            {
                                delta = new ListDelta<T>();
                                foreach (var add in state.Delta.Inserts)
                                {
                                    var index = cache.Cache.Count;
                                    delta.Insert(index, new[] { add });
                                    cache.Cache.Insert(index, add);
                                }
                                foreach (var remove in state.Delta.Deletes)
                                {
                                    var index = cache.Cache.IndexOf(remove);
                                    delta.Delete(cache.Cache.IndexOf(remove), new[] { remove });
                                    cache.Cache.RemoveAt(index);
                                }
                            }

                            // apply to cache
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