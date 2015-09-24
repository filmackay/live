using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveCollection<Tuple<T1, T2>> CrossJoin<T1, T1IDelta, T2, T2IDelta>
            (this ILiveCollection<T1, T1IDelta> source1, ILiveCollection<T2, T2IDelta> source2)
            where T1IDelta : class, ICollectionDelta<T1>
            where T2IDelta : class, ICollectionDelta<T2>
        {
            return source1.CrossJoin(source2, Tuple.Create);
        }

        public static ILiveCollection<TResult> CrossJoin<T1, T1IDelta, T2, T2IDelta, TResult>
            (this ILiveCollection<T1, T1IDelta> source1, ILiveCollection<T2, T2IDelta> source2, Func<T1, T2, TResult> selector)
            where T1IDelta : class, ICollectionDelta<T1>
            where T2IDelta : class, ICollectionDelta<T2>
        {
            return source1.SelectMany(s1 => source2.SelectStatic(s2 => selector(s1, s2)));
        }
    }
}
