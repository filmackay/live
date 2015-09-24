using System;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<TResult> Cast<TSource, TResult>(this ILiveList<TSource> innerList)
        {
            Func<TSource, TResult> selector;
            if (typeof(TResult).IsAssignableFrom(typeof(TSource)))
                selector = source => (TResult)(object)source;
            else
                selector = source => (TResult)Convert.ChangeType(source, typeof(TResult));
            return innerList.SelectStatic(selector);
        }

        public static ILiveList<TResult> OfType<TSource, TResult>(this ILiveList<TSource> innerList)
        {
            return innerList
                .Where(i => i is TResult)
                .SelectStatic(i => (TResult)(object)i);
        }
    }
}
