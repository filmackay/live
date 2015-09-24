using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<TResult> SelectStatic<TSource, TResult>(this ILiveCollection<TSource, IListDelta<TSource>> source, Func<TSource, TResult> selector)
        {
            var cacheList = new List<TResult>();
            var cache = new CollectionStateCache<TResult, IList<TResult>, IListDelta<TResult>>(cacheList);
            LiveObserver<ICollectionState<TSource, IListDelta<TSource>>> observer = null;

            return LiveListObservable<TResult>.Create(
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
                            var delta = state.Delta.ToListDelta<TSource, TResult>(
                                (newDelta, changes) =>
                                {
                                    foreach (var change in changes)
                                    {
                                        // apply deletes
                                        var deleteCount = change.Data.DeleteItems.Count();
                                        if (deleteCount > 0)
                                        {
                                            Debug.Assert(change.Index + deleteCount <= cacheList.Count);
                                            var resultDeletes = cacheList.Skip(change.Index).Take(deleteCount).ToArray();
                                            cacheList.RemoveRange(change.Index, deleteCount);
                                            newDelta.Delete(change.Index, resultDeletes);
                                        }

                                        // apply insert
                                        var insertCount = change.Data.InsertItems.Count();
                                        if (insertCount > 0)
                                        {
                                            Debug.Assert(change.Index <= cacheList.Count);
                                            var resultInserts = change.Data.InsertItems.Select(selector).ToArray();
                                            cacheList.InsertRange(change.Index, resultInserts);
                                            newDelta.Insert(change.Index, resultInserts);
                                        }
                                    }
                                });

                            // apply source state and delta to cache
                            cache.AddState(state.Status,
                                state.Inner == null ? null : state.Inner.Select(selector),
                                delta,
                                state.LastUpdated,
                                false);
                        }
                    }

                    // translate state
                    stateLock.DowngradeToReader();
                    return cache.Copy(stateLock);
                },
                () => observer.Dispose());
        }

        public static ILiveList<TResult> Select<TSource, TResult>(this ILiveCollection<TSource, IListDelta<TSource>> source, Func<TSource, ILiveValue<TResult>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }
    }
}
