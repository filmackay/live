using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<T> Filter<T, TIDelta>(this ILiveCollection<KeyValuePair<T, bool>, TIDelta> source)
            where TIDelta : ICollectionDelta<KeyValuePair<T, bool>>
        {
            IDisposable subscription = null;
            LiveObserver<ICollectionState<KeyValuePair<T, bool>, TIDelta>> observer = null;

            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    var state = observer.GetState(stateLock);

                    return state
                        .Extract
                            (true,
                             (inner, delta) =>
                                {
                                    if (delta == null)
                                        return null;
                                    var newDelta = new CollectionDelta<T>();
                                    if (delta.Deletes != null)
                                        newDelta.Delete(-1, delta.Deletes.Where(t => t.Value).Select(t => t.Key));
                                    if (delta.Inserts != null)
                                        newDelta.Insert(-1, delta.Inserts.Where(t => t.Value).Select(t => t.Key));
                                    return newDelta;
                                },
                            inner => inner
                                .Where(t => t.Value)
                                .Select(t => t.Key));
                },
                () => observer.Dispose());
        }

        public static ILiveCollection<T> Where<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<bool>> filter)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .Select(v => KeyValuePair.Create(v, filter(v)).Unwrap())
                .Filter();
        }

        public static ILiveCollection<T> Where<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, bool> filter)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .SelectStatic(v => KeyValuePair.Create(v, filter(v)))
                .Filter();
        }
    }
}
