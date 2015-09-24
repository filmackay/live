using System;

namespace Vertigo.Live
{
    public static partial class LiveTuple
    {
        public static ILiveValue<Tuple<T1, T2>> Create<T1, T2>(ILiveValue<T1> source1, T2 source2)
        {
            return
                new Func<T1, Tuple<T1, T2>>(t1 => Tuple.Create(t1, source2))
                    .LiveInvoke(source1);
        }

        public static ILiveValue<Tuple<T1, T2>> Unwrap<T1, T2>(this Tuple<ILiveValue<T1>, T2> source)
        {
            return Create(source.Item1, source.Item2);
        }

        public static ILiveValue<Tuple<T1, T2>> Create<T1, T2>(T1 source1, ILiveValue<T2> source2)
        {
            return
                new Func<T2, Tuple<T1, T2>>(t2 => Tuple.Create(source1, t2))
                    .LiveInvoke(source2);
        }

        public static ILiveValue<Tuple<T1, T2>> Unwrap<T1, T2>(this Tuple<T1, ILiveValue<T2>> source)
        {
            return Create(source.Item1, source.Item2);
        }
    }
 }