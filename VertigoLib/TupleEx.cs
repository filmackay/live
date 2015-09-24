using System;

namespace Vertigo
{
    public static class TupleEx
    {
        public static Tuple<T1, T2, T3> Append<T1, T2, T3>(this Tuple<T1, T2> source, T3 value)
        {
            return Tuple.Create(source.Item1, source.Item2, value);
        }

        public static Tuple<T1, T2, T3, T4> Append<T1, T2, T3, T4>(this Tuple<T1, T2, T3> source, T4 value)
        {
            return Tuple.Create(source.Item1, source.Item2, source.Item3, value);
        }
    }
}
