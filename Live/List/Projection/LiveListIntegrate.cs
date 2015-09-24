using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveList<Tuple<TKey, TData, TData>> Integrate<TKey, TData>(this ILiveList<Tuple<TKey, TData>> source1, ILiveList<Tuple<TKey, TData>> source2)
            where TKey : IComparable<TKey>
        {
            LiveObserver<ICollectionState<Tuple<TKey, TData>, IListDelta<Tuple<TKey, TData>>>> observer1 = null;
            LiveObserver<ICollectionState<Tuple<TKey, TData>, IListDelta<Tuple<TKey, TData>>>> observer2 = null;

            return LiveListObservable<Tuple<TKey, TData, TData>>.Create(
                innerChanged =>
                {
                    source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
                    source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
                    return new[] {observer1.GetStateLock, observer2.GetStateLock}.MergeLockers();
                },
                (innerChanged, notified, stateLock, oldState) => null,
                () =>
                {
                    observer1.Dispose();
                    observer2.Dispose();
                });
        }
    }
}
