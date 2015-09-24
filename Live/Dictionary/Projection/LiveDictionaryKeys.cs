using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveSet<TKey> Keys<TKey,TValue>(this ILiveDictionary<TKey, TValue> source)
        {
            IDisposable subscription = null;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> observer = null;

            return LiveSetObservable<TKey>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                    observer
                        .GetState(stateLock)
                        .Extract(
                            true,
                            (inner, delta) => delta.ToSetDelta(i => i.Select(kv => kv.Key)),
                            inner => inner.Select(kv => kv.Key))
                ,
                () => observer.Dispose());
        }
    }
}
