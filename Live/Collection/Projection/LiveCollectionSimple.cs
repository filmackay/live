using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveCollection<TResult> Cast<TSource, TIDelta, TResult>(this ILiveCollection<TSource, TIDelta> inner)
            where TIDelta : class, ICollectionDelta<TSource>
        {
            Func<TSource, TResult> selector;
            if (typeof(TResult).IsAssignableFrom(typeof(TSource)))
                selector = source => (TResult)(object)source;
            else
                selector = source => (TResult)System.Convert.ChangeType(source, typeof(TResult));
            return inner.SelectStatic(selector);
        }

        public static ILiveCollection<TResult> OfType<TSource, TIDelta, TResult>(this ILiveCollection<TSource, TIDelta> inner)
            where TIDelta : class, ICollectionDelta<TSource>
        {
            return inner
                .Where(i => i is TResult)
                .SelectStatic(i => (TResult)(object)i);
        }
    }
}
