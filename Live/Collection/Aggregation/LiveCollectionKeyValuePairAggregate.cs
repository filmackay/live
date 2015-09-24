using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveValue<KeyValuePair<TKey, TValue>> MaxByKey<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<TKey, TValue>, TIDelta> source)
            where TIDelta : ICollectionDelta<KeyValuePair<TKey, TValue>>
        {
            return source.Aggregate<KeyValuePair<TKey, TValue>, TIDelta, KeyValuePair<TKey, TValue>>(
                (max, state) =>
                {
                    if (state.Status.IsConnecting())
                        return state.Inner.MaxBy(kv => kv.Key).First();
                    if (state.Delta.HasChange())
                    {
                        if (state.Delta.Inserts != null)
                        {
                            var addMax = state.Delta.Inserts.MaxBy(kv => kv.Key).First();
                            if (Comparer<TKey>.Default.Compare(addMax.Key, max.Key) > 0)
                            {
                                // we know this is the new max
                                return addMax;
                            }
                        }

                        if (state.Delta.Deletes != null && state.Delta.Deletes.Contains(max))
                        {
                            // we no longer have the aggregate
                            return state.Inner.MaxBy(kv => kv.Key).First();
                        }
                    }

                    // unchanged
                    return max;
                });
        }

        public static ILiveValue<TValue> MaxBy<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, ILiveValue<TKey>> selector)
            where TIDelta : ICollectionDelta<TValue>
        {
            return source
                .Select(value => LiveKeyValuePair.Create(selector(value), value))
                .MaxByKey()
                .SelectStatic(kv => kv.Value);
        }

        public static ILiveValue<TValue> MaxByStatic<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, TKey> selector)
            where TIDelta : ICollectionDelta<TValue>
        {
            return source
                .SelectStatic(value => KeyValuePair.Create(selector(value), value))
                .MaxByKey()
                .SelectStatic(kv => kv.Value);
        }

        public static ILiveValue<KeyValuePair<TKey, TValue>> MinByKey<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<TKey, TValue>, TIDelta> source)
            where TIDelta : ICollectionDelta<KeyValuePair<TKey, TValue>>
        {
            return source.Aggregate<KeyValuePair<TKey, TValue>, TIDelta, KeyValuePair<TKey, TValue>>(
                (min, state) =>
                {
                    if (state.Status.IsConnecting())
                    {
                        return state.Inner.MinBy(kv => kv.Key).First();
                    }
                    if (state.Delta != null)
                    {
                        if (state.Delta.Inserts.Any())
                        {
                            var addMin = state.Delta.Inserts.MinBy(kv => kv.Key).First();
                            if (Comparer<TKey>.Default.Compare(addMin.Key, min.Key) < 0)
                            {
                                // we know this is the new aggregate
                                return addMin;
                            }
                        }

                        if (state.Delta.Deletes.Contains(min))
                        {
                            // we no longer have the aggregate
                            return state.Inner.MinBy(kv => kv.Key).First();
                        }
                    }

                    // unchanged
                    return min;
                });
        }

        public static ILiveValue<TValue> MinBy<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, ILiveValue<TKey>> selector)
            where TIDelta : ICollectionDelta<TValue>
        {
            return source
                .Select(value => LiveKeyValuePair.Create(selector(value), value))
                .MinByKey()
                .SelectStatic(kv => kv.Value);
        }

        public static ILiveValue<TValue> MinByStatic<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, TKey> selector)
            where TIDelta : ICollectionDelta<TValue>
        {
            return source
                .SelectStatic(value => KeyValuePair.Create(selector(value), value))
                .MinByKey()
                .SelectStatic(kv => kv.Value);
        }
    }
}