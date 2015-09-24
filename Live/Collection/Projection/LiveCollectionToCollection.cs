using System;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<T> ToLiveCollection<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            // handle LiveList
            if (source is ILiveCollection<T>)
                return source as ILiveCollection<T>;

            // generic collection
            LiveObserver<ICollectionState<T, TIDelta>> observer = null;
            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                    observer
                        .GetState(stateLock)
                        .Extract(
                            true,
                            (inner, oldDelta) => oldDelta.ToCollectionDelta(i => i),
                            inner => inner),
                () => observer.Dispose());
        }
    }
}