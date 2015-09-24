using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Return<T>(ILiveValue<T> source)
        {
            var observer = default(LiveObserver<IValueState<T>>);
            return LiveListObservable<T>.Create(
                innerChanged =>
                    {
                        observer = source.CreateObserver(innerChanged);
                        return Lockers.Empty;
                    },
                (innerChanged, notified, stateLock, oldState) =>
                    {
                        var state = observer.GetState();
                        var newStatus = oldState.GetStatus().Add(state.Status);

                        var delta = default(ListDelta<T>);
                        if (newStatus.IsDeltaRelevant())
                        {
                            delta = new ListDelta<T>();
                            delta.Delete(0, new[] { state.OldValue });
                            delta.Insert(0, new[] { state.NewValue });
                        }

                        var ret = new CollectionState<T, IListDelta<T>, IList<T>>();
                        ret.AddState(
                            newStatus,
                            newStatus.IsConnected() ? new[] { state.NewValue } : null,
                            delta,
                            state.LastUpdated,
                            stateLock);
                        return ret;
                    },
                () => observer.Dispose());
        }
    }
}
