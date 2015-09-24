using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveKeyValuePair
    {
        public static ILiveValue<KeyValuePair<TKey, TValue>> Create<TKey, TValue>(ILiveValue<TKey> key, ILiveValue<TValue> value)
        {
            return
                new Func<TKey, TValue, KeyValuePair<TKey, TValue>>(KeyValuePair.Create)
                    .LiveInvoke(key, value);
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> Create<TKey, TValue>(TKey key, ILiveValue<TValue> value)
        {
            return
                new Func<TValue, KeyValuePair<TKey, TValue>>(v => KeyValuePair.Create(key, v))
                    .LiveInvoke(value);
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> Create<TKey, TValue>(ILiveValue<TKey> key, TValue value)
        {
            return
                new Func<TKey, KeyValuePair<TKey, TValue>>(k => KeyValuePair.Create(k, value))
                    .LiveInvoke(key);
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue>(this KeyValuePair<ILiveValue<TKey>, ILiveValue<TValue>> source)
        {
            return Create(source.Key, source.Value);
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue>(this KeyValuePair<ILiveValue<TKey>, TValue> source)
        {
            return source
                .Key
                .SelectStatic(k => KeyValuePair.Create(k, source.Value));
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue>(this KeyValuePair<TKey, ILiveValue<TValue>> source)
        {
            return source
                .Value
                .SelectStatic(v => KeyValuePair.Create(source.Key, v));
        }

        public static ILiveCollection<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<ILiveValue<TKey>, ILiveValue<TValue>>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<ILiveValue<TKey>, ILiveValue<TValue>>>
        {
            return source.Select(kv => kv.Unwrap());
        }

        public static ILiveCollection<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<ILiveValue<TKey>, TValue>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<ILiveValue<TKey>, TValue>>
        {
            return source.Select(kv => kv.Unwrap());
        }

        public static ILiveSet<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue>(this ILiveSet<KeyValuePair<ILiveValue<TKey>, TValue>> source)
        {
            return source
                .Select(kv => kv.Unwrap())
                .AsLiveSet();
        }

        public static ILiveCollection<KeyValuePair<TKey, TValue>> Unwrap<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<TKey, ILiveValue<TValue>>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, ILiveValue<TValue>>>
        {
            return source.Select(kv => kv.Unwrap());
        }
    }
}
