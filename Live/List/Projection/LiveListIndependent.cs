using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> ToIndependent<T>(this ILiveList<T> source)
        {
            var observer = default(LiveObserver<ICollectionState<T, IListDelta<T>>>);

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get state
                    using (var state = observer.GetState(stateLock))
                    {
                        // create independent copy
                        var independentState = new CollectionState<T, IListDelta<T>, IList<T>>();
                        independentState.SetState(state.Status,
                            state.Delta,
                            state.Inner == null ? null : state.Inner.ToArray(),
                            state.LastUpdated,
                            null);
                        return independentState;
                    }
                },
                () => observer.Dispose());
        }
    }
}
