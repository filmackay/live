using System;

namespace Vertigo.Live
{
    public static partial class LiveTuple
    {
		public static ILiveValue<Tuple<T1>> Create<T1>(ILiveValue<T1> source1)
        {
			return
				new Func<T1, Tuple<T1>>(Tuple.Create)
					.LiveInvoke(source1);
		}

		public static ILiveValue<Tuple<T1>> Unwrap<T1>(this Tuple<ILiveValue<T1>> source)
        {
			return Create(source.Item1);
		}

		public static ILiveValue<Tuple<T1,T2>> Create<T1,T2>(ILiveValue<T1> source1, ILiveValue<T2> source2)
        {
			return
				new Func<T1,T2, Tuple<T1,T2>>(Tuple.Create)
					.LiveInvoke(source1, source2);
		}

		public static ILiveValue<Tuple<T1,T2>> Unwrap<T1,T2>(this Tuple<ILiveValue<T1>, ILiveValue<T2>> source)
        {
			return Create(source.Item1, source.Item2);
		}

		public static ILiveValue<Tuple<T1,T2,T3>> Create<T1,T2,T3>(ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3)
        {
			return
				new Func<T1,T2,T3, Tuple<T1,T2,T3>>(Tuple.Create)
					.LiveInvoke(source1, source2, source3);
		}

		public static ILiveValue<Tuple<T1,T2,T3>> Unwrap<T1,T2,T3>(this Tuple<ILiveValue<T1>, ILiveValue<T2>, ILiveValue<T3>> source)
        {
			return Create(source.Item1, source.Item2, source.Item3);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4>> Create<T1,T2,T3,T4>(ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4)
        {
			return
				new Func<T1,T2,T3,T4, Tuple<T1,T2,T3,T4>>(Tuple.Create)
					.LiveInvoke(source1, source2, source3, source4);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4>> Unwrap<T1,T2,T3,T4>(this Tuple<ILiveValue<T1>, ILiveValue<T2>, ILiveValue<T3>, ILiveValue<T4>> source)
        {
			return Create(source.Item1, source.Item2, source.Item3, source.Item4);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5>> Create<T1,T2,T3,T4,T5>(ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5)
        {
			return
				new Func<T1,T2,T3,T4,T5, Tuple<T1,T2,T3,T4,T5>>(Tuple.Create)
					.LiveInvoke(source1, source2, source3, source4, source5);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5>> Unwrap<T1,T2,T3,T4,T5>(this Tuple<ILiveValue<T1>, ILiveValue<T2>, ILiveValue<T3>, ILiveValue<T4>, ILiveValue<T5>> source)
        {
			return Create(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5,T6>> Create<T1,T2,T3,T4,T5,T6>(ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6)
        {
			return
				new Func<T1,T2,T3,T4,T5,T6, Tuple<T1,T2,T3,T4,T5,T6>>(Tuple.Create)
					.LiveInvoke(source1, source2, source3, source4, source5, source6);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5,T6>> Unwrap<T1,T2,T3,T4,T5,T6>(this Tuple<ILiveValue<T1>, ILiveValue<T2>, ILiveValue<T3>, ILiveValue<T4>, ILiveValue<T5>, ILiveValue<T6>> source)
        {
			return Create(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5,T6,T7>> Create<T1,T2,T3,T4,T5,T6,T7>(ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7)
        {
			return
				new Func<T1,T2,T3,T4,T5,T6,T7, Tuple<T1,T2,T3,T4,T5,T6,T7>>(Tuple.Create)
					.LiveInvoke(source1, source2, source3, source4, source5, source6, source7);
		}

		public static ILiveValue<Tuple<T1,T2,T3,T4,T5,T6,T7>> Unwrap<T1,T2,T3,T4,T5,T6,T7>(this Tuple<ILiveValue<T1>, ILiveValue<T2>, ILiveValue<T3>, ILiveValue<T4>, ILiveValue<T5>, ILiveValue<T6>, ILiveValue<T7>> source)
        {
			return Create(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);
		}

	}
}

