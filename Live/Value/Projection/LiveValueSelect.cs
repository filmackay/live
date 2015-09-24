using System;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        // static results
        public static ILiveValue<TResult> SelectStaticState<TSource, TResult>(this ILiveValue<TSource> source, Func<IValueState<TSource>, TResult> selector)
        {
            return selector.LiveInvoke(source);
        }

        public static ILiveValue<TResult> SelectStatic<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, TResult> selector)
        {
            return selector.LiveInvoke(source);
        }

        // live value results
        public static ILiveValue<TResult> Select<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, ILiveValue<TResult>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        // live collection results
        public static ILiveCollection<TInner, TInnerIDelta> Select<TOuter, TInner, TInnerIDelta>(this ILiveValue<TOuter> source, Func<TOuter, ILiveCollection<TInner, TInnerIDelta>> selector)
            where TInnerIDelta : ICollectionDelta<TInner>
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        public static ILiveCollection<TInner> Select<TOuter, TInner>(this ILiveValue<TOuter> source, Func<TOuter, ILiveCollection<TInner>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        public static ILiveSet<TResult> Select<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, ILiveSet<TResult>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        public static ILiveList<TResult> Select<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, ILiveList<TResult>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        public static ILiveDictionary<TKey, TValue> Select<TSource, TKey, TValue>(this ILiveValue<TSource> source, Func<TSource, ILiveDictionary<TKey, TValue>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }

        public static IObservable<TResult> Select<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, IObservable<TResult>> selector)
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }
    }
}
