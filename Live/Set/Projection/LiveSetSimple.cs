using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveSet<TResult> Cast<TSource, TResult>(this ILiveSet<TSource> source)
        {
            Func<TSource, ILiveValue<TResult>> selector;
            if (typeof(TResult).IsAssignableFrom(typeof(TSource)))
                selector = s => ((TResult)(object)s).ToLiveConst();
            else
                selector = s => ((TResult)System.Convert.ChangeType(s, typeof(TResult))).ToLiveConst();
            return source
                .Select(selector)
                .AsLiveSet();
        }

        public static ILiveSet<TResult> OfType<TSource, TResult>(this ILiveSet<TSource> innerSet)
        {
            return innerSet
                .Where(i => i is TResult)
                .Select(i => ((TResult)(object)i).ToLiveConst())
                .AsLiveSet();
        }
    }
}
