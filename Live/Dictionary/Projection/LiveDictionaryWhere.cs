using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveDictionary<TKey, TValue> Filter<TKey, TValue>(this ILiveDictionary<TKey, Tuple<bool, TValue>> source)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, Tuple<bool, TValue>>, IDictionaryDelta<TKey, Tuple<bool, TValue>>>> observer = null;

            return LiveDictionaryObservable<TKey, TValue>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    var state = observer.GetState(stateLock);
                    return state
                        .Extract
                            (true,
                             (inner, delta) => delta.ToDictionaryDelta(items => items.Where(kv => kv.Value.Item1).Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Item2))),
                             inner => inner
                                 .Where(kv => kv.Value.Item1)
                                 .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Item2)));
                },
                () => observer.Dispose());
        }

        public static ILiveDictionary<TKey, TValue> Where<TKey, TValue>(this ILiveDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> filter)
        {
            return source
                .SelectDictionaryStatic(kv => new Tuple<bool, TValue>(filter(kv), kv.Value))
                .Filter();
        }

        public static ILiveDictionary<TKey, TValue> Where<TKey, TValue>(this ILiveDictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, ILiveValue<bool>> filter)
        {
            return source
                .SelectDictionary(kv => new Tuple<ILiveValue<bool>, TValue>(filter(kv), kv.Value).Unwrap())
                .Filter();
        }
    }
}
