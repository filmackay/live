using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        // this gives collection states independence of the Inner collection, useful for testing purposes
        public static ILiveCollection<T> ToIndependent<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            LiveObserver<ICollectionState<T, TIDelta>> observer = null;

            return LiveCollectionObservable<T>.Create(
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
                        var independentState = new CollectionState<T, ICollectionDelta<T>, ICollection<T>>();
                        independentState.SetState(state.Status,
                            state.Delta.ToCollectionDelta(item => item),
                            state.Inner.ToArray(),
                            state.LastUpdated,
                            null);
                        return independentState;
                    }
                },
                () => observer.Dispose());
        }
    }
}
