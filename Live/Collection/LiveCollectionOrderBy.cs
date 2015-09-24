using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveList<TValue> OrderBy<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, ILiveValue<TKey>> keySelector)
            where TIDelta : class, ICollectionDelta<TValue>
        {
            return source
                .Select(v => LiveKeyValuePair.Create(keySelector(v), v))
                .Sort();
        }

        public static ILiveList<TValue> OrderByDescending<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, ILiveValue<TKey>> keySelector)
            where TIDelta : class, ICollectionDelta<TValue>
        {
            return source
                .OrderBy(keySelector)
                .Reverse();
        }

        public static ILiveList<TValue> OrderByStatic<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, TKey> keySelector)
            where TIDelta : class, ICollectionDelta<TValue>
        {
            return source
                .SelectStatic(v => KeyValuePair.Create(keySelector(v), v))
                .Sort();
        }

        public static ILiveList<TValue> OrderByDescendingStatic<TKey, TValue, TIDelta>(this ILiveCollection<TValue, TIDelta> source, Func<TValue, TKey> keySelector)
            where TIDelta : class, ICollectionDelta<TValue>
        {
            return source
                .OrderByStatic(keySelector)
                .Reverse();
        }
    }
}
