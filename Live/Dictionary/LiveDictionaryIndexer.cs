using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveValue<TValue> Value<TKey, TValue>(this ILiveDictionary<TKey, TValue> dictionary, ILiveValue<TKey> key)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> observer = null;
            LiveObserver<IValueState<TKey>> keyObserver = null;

            return LiveValueObservable<TValue>.Create(
                innerChanged =>
                {
                    dictionary.Subscribe(observer = dictionary.CreateObserver(innerChanged));
                    key.Subscribe(keyObserver = key.CreateObserver(innerChanged));
                },
                () =>
                {
                    observer.GetNotify();
                    keyObserver.GetNotify();
                },
                (innerChanged, oldState) =>
                {
                    // get key and dictionary
                    var result = new ValueState<TValue>();
                    using (var dictionaryState = observer.GetState())
                    {
                        var keyState = keyObserver.GetState();
                        result.Status = keyState.Status.And(dictionaryState.Status);
                        if (result.Status.IsConnected())
                        {
                            ((IDictionary<TKey, TValue>)dictionaryState.Inner).TryGetValue(keyState.NewValue, out result.NewValue);
                            result.LastUpdated = Math.Max(dictionaryState.LastUpdated, keyState.LastUpdated);
                        }
                    }
                    return result;
                },
                () =>
                {
                    observer.Dispose();
                    keyObserver.Dispose();
                });
        }

        public static bool TryGetValue<TKey, TValue>(this ILiveDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            var outValue = default(TValue);
            var found = source.UseInner(inner => inner.TryGetValue(key, out outValue));
            value = outValue;
            return found;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this ILiveDictionary<TKey, TValue> source, TKey key)
        {
            var outValue = default(TValue);
            source.UseInner(inner => inner.TryGetValue(key, out outValue));
            return outValue;
        }
    }
}