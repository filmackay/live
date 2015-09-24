using System;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveValue<bool> Contains<T>(this ILiveSet<T> source, ILiveValue<T> sourceItem)
        {
            LiveObserver<ICollectionState<T, ISetDelta<T>>> observer = null;
            LiveObserver<IValueState<T>> observerItem = null;

            return LiveValueObservable<bool>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    sourceItem.Subscribe(observerItem = sourceItem.CreateObserver(innerChanged));
                },
                () =>
                {
                    observer.GetNotify();
                    observerItem.GetNotify();
                },
                (innerChanged, oldState) =>
                {
                    var itemChange = observerItem.GetState();
                    using (var state = observer.GetState())
                    {
                        return oldState.Add(
                            new ValueState<bool>
                                {
                                    LastUpdated = state.LastUpdated,
                                    NewValue = state.Inner.Contains(itemChange.NewValue),
                                    Status = itemChange.Status.And(state.Status),
                                });
                    }
                },
                () =>
                {
                    observer.Dispose();
                    observerItem.Dispose();
                });
        }
    }
}