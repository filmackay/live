using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveList<T> Reverse<T>(this ILiveCollection<T, IListDelta<T>> source)
        {
            LiveObserver<ICollectionState<T, IListDelta<T>>> observer = null;

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    using (var state = observer.GetState(stateLock))
                    {
                        return state
                            .Extract<T, IListDelta<T>, T, IListDelta<T>, IList<T>>
                               (true,
                                (inner, delta) =>
                                {
                                    if (delta == null)
                                        return null;
                                    return delta.ToListDelta<T, T>(
                                        (newDelta, changes) =>
                                        {
                                            var newCount = state.Inner.Count();
                                            foreach (var change in changes.Reverse())
                                            {
                                                // reverse index of change
                                                var deleteCount = change.Data.DeleteItems.Count();
                                                var insertCount = change.Data.InsertItems.Count();
                                                if (deleteCount > 0)
                                                    newDelta.Delete(newCount - change.Index, change.Data.DeleteItems);
                                                if (insertCount > 0)
                                                    newDelta.Insert(newCount - change.Index - 1, change.Data.InsertItems);
                                            }
                                        });
                                },
                                Enumerable.Reverse);
                    }
                },
                () => observer.Dispose());
        }
    }
}
