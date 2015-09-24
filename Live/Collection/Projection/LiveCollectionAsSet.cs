using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveSet<T> AsLiveSet<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : class, ICollectionDelta<T>
        {
            var cache = new CollectionStateCache<T, ISet<T>, ISetDelta<T>>(new HashSet<T>());
            IDisposable subscription = null;
            LiveObserver<ICollectionState<T, TIDelta>> observer = null;

            return LiveSetObservable<T>.Create(
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
                            // apply source state to cache
                            cache.AddState(state.Status,
                                state.Inner,
                                state.Delta.ToSetDelta(items => items),
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
    }
}
