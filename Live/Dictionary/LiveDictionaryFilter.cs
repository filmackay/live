using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveSet<T> Filter<T>(this ILiveDictionary<T, bool> source)
        {
            LiveObserver<ICollectionState<KeyValuePair<T, bool>, IDictionaryDelta<T, bool>>> observer = null;

            return LiveSetObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    var state = observer
                        .GetState(stateLock);

                    return state
                        .Extract
                        (true,
                         (inner, delta) => delta.ToSetDelta(items => items.Where(t => t.Value).Select(t => t.Key)),
                         inner => inner.Where(t => t.Value).Select(t => t.Key));
                },
                () => observer.Dispose());
        }
    }
}