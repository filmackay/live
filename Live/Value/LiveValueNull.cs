using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveValue<T?> ToLiveNullable<T>(this ILiveValue<T> source)
            where T : struct
        {
            return source.SelectStatic(i => (T?)i);
        }

        public static ILiveValue<T> DeNull<T>(this ILiveValue<T?> source, T onNull)
            where T : struct
        {
            return source.SelectStatic(l => l.HasValue ? l.Value : onNull);
        }

        public static ILiveValue<T> DeNull<T>(this ILiveValue<T?> source, ILiveValue<T> onNull)
            where T : struct
        {
            return ExtensionsStruct<T>.OnNull(source, onNull);
        }

        public static ILiveValue<T?> OnNull<T>(this ILiveValue<T?> source, ILiveValue<T?> onNull)
            where T : struct
        {
            return ExtensionsStruct<T>.OnNull2(source, onNull);
        }

        public static ILiveValue<T> DeNull<T>(this ILiveValue<T> source, T onNull)
            where T : class
        {
            return source.SelectStatic(l => l ?? onNull);
        }

        public static ILiveValue<T> DeNull<T>(this ILiveValue<T> source, ILiveValue<T> onNull)
            where T : class
        {
            return ExtensionsClass<T>.OnNull(source, onNull);
        }

        public static ILiveValue<TResult> NullSelect<TSource, TResult>(this ILiveValue<TSource?> source, Func<ILiveValue<TSource>, ILiveValue<TResult>> notNullSelector, ILiveValue<TResult> onNull)
            where TSource : struct
        {
            return source
                .HasValue()
                .If(notNullSelector(source.GetValueOrDefault()), onNull);
        }

        public static ILiveValue<TResult> NullSelect<TSource, TResult>(this ILiveValue<TSource?> source, Func<TSource, ILiveValue<TResult>> notNullSelector, ILiveValue<TResult> onNull)
            where TSource : struct
        {
            return source
                .SelectStatic(s => s.HasValue ? notNullSelector(s.Value) : onNull)
                .Unwrap();
        }

        public static ILiveValue<TResult> NullSelect<TSource, TResult>(this ILiveValue<TSource?> source, Func<TSource, ILiveValue<TResult>> notNullSelector, TResult onNull = default(TResult))
            where TSource : struct
        {
            return source
                .SelectStatic(s => s.HasValue ? notNullSelector(s.Value) : onNull.ToLiveConst())
                .Unwrap();
        }

        public static ILiveValue<TResult> NullSelect<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, ILiveValue<TResult>> notNullSelector, ILiveValue<TResult> onNull)
            where TSource : class
        {
            return source
                .SelectStatic(s => s != null ? notNullSelector(s) : onNull)
                .Unwrap();
        }

        public static ILiveValue<TResult> NullSelectStatic<TSource, TResult>(this ILiveValue<TSource?> source, Func<TSource, TResult> selector, TResult onNull = default(TResult))
            where TSource : struct
        {
            return source
                .SelectStatic(s => s.HasValue ? selector(s.Value) : onNull);
        }

        public static ILiveValue<TResult?> NullPassthruSelectStatic<TSource, TResult>(this ILiveValue<TSource?> source, Func<TSource, TResult> selector)
            where TSource : struct
            where TResult : struct
        {
            return source
                .SelectStatic(s => s.HasValue ? selector(s.Value) : (TResult?)null);
        }

        public static ILiveValue<TResult> NullSelectStatic<TSource, TResult>(this ILiveValue<TSource> source, Func<TSource, TResult> selector, TResult onNull = default(TResult))
            where TSource : class
        {
            return source
                .SelectStatic(s => s != null ? selector(s) : onNull);
        }
    }

    public static partial class ExtensionsStruct<T>
    where T : struct
    {
        public static readonly LiveFunc<T?, T, T> OnNull = new Func<T?, T, T>((s, on) => s.HasValue ? s.Value : on).Create();
        public static readonly LiveFunc<T?, T?, T?> OnNull2 = new Func<T?, T?, T?>((s, on) => s.HasValue ? s : on).Create();
    }

    public static partial class ExtensionsClass<T>
        where T : class
    {
        public static readonly LiveFunc<T, T, T> OnNull = new Func<T, T, T>((s, on) => s ?? on).Create();
    }
}