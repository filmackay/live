using System;

namespace Vertigo.Live
{
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1, TResult>(this Func<IValueState<T0>, IValueState<T1>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status),
							LastUpdated = Math.Max(state0.LastUpdated, state1.LastUpdated),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1, TResult>(this Func<T0, T1, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, TResult>((state0, state1) => func(state0.NewValue, state1.NewValue))
                    .LiveInvoke(source0,source1);
        }

        public static LiveFunc<T0, T1, TResult> Create<T0,T1, TResult>(this Func<T0, T1, TResult> func)
        {
            return (t0, t1) => func.LiveInvoke(t0, t1);
        }

		public static ILiveValue<TResult> Join<T0,T1, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, Func<T0, T1, TResult> selector)
        {
            return selector.Create()(source0, source1);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status)),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, state2.LastUpdated)),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2, TResult>(this Func<T0, T1, T2, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, TResult>((state0, state1, state2) => func(state0.NewValue, state1.NewValue, state2.NewValue))
                    .LiveInvoke(source0,source1,source2);
        }

        public static LiveFunc<T0, T1, T2, TResult> Create<T0,T1,T2, TResult>(this Func<T0, T1, T2, TResult> func)
        {
            return (t0, t1, t2) => func.LiveInvoke(t0, t1, t2);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, Func<T0, T1, T2, TResult> selector)
        {
            return selector.Create()(source0, source1, source2);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, state3.LastUpdated))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3, TResult>(this Func<T0, T1, T2, T3, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, TResult>((state0, state1, state2, state3) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue))
                    .LiveInvoke(source0,source1,source2,source3);
        }

        public static LiveFunc<T0, T1, T2, T3, TResult> Create<T0,T1,T2,T3, TResult>(this Func<T0, T1, T2, T3, TResult> func)
        {
            return (t0, t1, t2, t3) => func.LiveInvoke(t0, t1, t2, t3);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, Func<T0, T1, T2, T3, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status)))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, state4.LastUpdated)))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4, TResult>(this Func<T0, T1, T2, T3, T4, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, TResult>((state0, state1, state2, state3, state4) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, TResult> Create<T0,T1,T2,T3,T4, TResult>(this Func<T0, T1, T2, T3, T4, TResult> func)
        {
            return (t0, t1, t2, t3, t4) => func.LiveInvoke(t0, t1, t2, t3, t4);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, Func<T0, T1, T2, T3, T4, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, state5.LastUpdated))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5, TResult>(this Func<T0, T1, T2, T3, T4, T5, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, TResult>((state0, state1, state2, state3, state4, state5) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, TResult> Create<T0,T1,T2,T3,T4,T5, TResult>(this Func<T0, T1, T2, T3, T4, T5, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5) => func.LiveInvoke(t0, t1, t2, t3, t4, t5);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, Func<T0, T1, T2, T3, T4, T5, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status)))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, state6.LastUpdated)))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, TResult>((state0, state1, state2, state3, state4, state5, state6) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, TResult> Create<T0,T1,T2,T3,T4,T5,T6, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, Func<T0, T1, T2, T3, T4, T5, T6, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, state7.LastUpdated))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status)))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, state8.LastUpdated)))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, state9.LastUpdated))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status)))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, state10.LastUpdated)))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10, ILiveValue<T11> t11);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;
			LiveObserver<IValueState<T11>> observer11 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
					source11.Subscribe(observer11 = source11.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
					observer11.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();
					var state11 = observer11.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status.And(state11.Status))))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, Math.Max(state10.LastUpdated, state11.LastUpdated))))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
					observer11.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue, state11.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10,source11);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10, ILiveValue<T11> t11, ILiveValue<T12> t12);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;
			LiveObserver<IValueState<T11>> observer11 = null;
			LiveObserver<IValueState<T12>> observer12 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
					source11.Subscribe(observer11 = source11.CreateObserver(innerChanged));
					source12.Subscribe(observer12 = source12.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
					observer11.GetNotify();
					observer12.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();
					var state11 = observer11.GetState();
					var state12 = observer12.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status.And(state11.Status.And(state12.Status)))))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, Math.Max(state10.LastUpdated, Math.Max(state11.LastUpdated, state12.LastUpdated)))))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
					observer11.Dispose();
					observer12.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue, state11.NewValue, state12.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10,source11,source12);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10, ILiveValue<T11> t11, ILiveValue<T12> t12, ILiveValue<T13> t13);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;
			LiveObserver<IValueState<T11>> observer11 = null;
			LiveObserver<IValueState<T12>> observer12 = null;
			LiveObserver<IValueState<T13>> observer13 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
					source11.Subscribe(observer11 = source11.CreateObserver(innerChanged));
					source12.Subscribe(observer12 = source12.CreateObserver(innerChanged));
					source13.Subscribe(observer13 = source13.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
					observer11.GetNotify();
					observer12.GetNotify();
					observer13.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();
					var state11 = observer11.GetState();
					var state12 = observer12.GetState();
					var state13 = observer13.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status.And(state11.Status.And(state12.Status.And(state13.Status))))))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, Math.Max(state10.LastUpdated, Math.Max(state11.LastUpdated, Math.Max(state12.LastUpdated, state13.LastUpdated))))))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
					observer11.Dispose();
					observer12.Dispose();
					observer13.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue, state11.NewValue, state12.NewValue, state13.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10,source11,source12,source13);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10, ILiveValue<T11> t11, ILiveValue<T12> t12, ILiveValue<T13> t13, ILiveValue<T14> t14);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, IValueState<T14>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;
			LiveObserver<IValueState<T11>> observer11 = null;
			LiveObserver<IValueState<T12>> observer12 = null;
			LiveObserver<IValueState<T13>> observer13 = null;
			LiveObserver<IValueState<T14>> observer14 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
					source11.Subscribe(observer11 = source11.CreateObserver(innerChanged));
					source12.Subscribe(observer12 = source12.CreateObserver(innerChanged));
					source13.Subscribe(observer13 = source13.CreateObserver(innerChanged));
					source14.Subscribe(observer14 = source14.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
					observer11.GetNotify();
					observer12.GetNotify();
					observer13.GetNotify();
					observer14.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();
					var state11 = observer11.GetState();
					var state12 = observer12.GetState();
					var state13 = observer13.GetState();
					var state14 = observer14.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status.And(state11.Status.And(state12.Status.And(state13.Status.And(state14.Status)))))))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, Math.Max(state10.LastUpdated, Math.Max(state11.LastUpdated, Math.Max(state12.LastUpdated, Math.Max(state13.LastUpdated, state14.LastUpdated)))))))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13, state14)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
					observer11.Dispose();
					observer12.Dispose();
					observer13.Dispose();
					observer14.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, IValueState<T14>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13, state14) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue, state11.NewValue, state12.NewValue, state13.NewValue, state14.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10,source11,source12,source13,source14);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14);
        }
    }
    
    public delegate ILiveValue<TResult> LiveFunc<in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, out TResult>(ILiveValue<T0> t0, ILiveValue<T1> t1, ILiveValue<T2> t2, ILiveValue<T3> t3, ILiveValue<T4> t4, ILiveValue<T5> t5, ILiveValue<T6> t6, ILiveValue<T7> t7, ILiveValue<T8> t8, ILiveValue<T9> t9, ILiveValue<T10> t10, ILiveValue<T11> t11, ILiveValue<T12> t12, ILiveValue<T13> t13, ILiveValue<T14> t14, ILiveValue<T15> t15);

    public static partial class LiveFunc
    {
	    public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15, TResult>(this Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, IValueState<T14>, IValueState<T15>, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14, ILiveValue<T15> source15)
		{
			LiveObserver<IValueState<T0>> observer0 = null;
			LiveObserver<IValueState<T1>> observer1 = null;
			LiveObserver<IValueState<T2>> observer2 = null;
			LiveObserver<IValueState<T3>> observer3 = null;
			LiveObserver<IValueState<T4>> observer4 = null;
			LiveObserver<IValueState<T5>> observer5 = null;
			LiveObserver<IValueState<T6>> observer6 = null;
			LiveObserver<IValueState<T7>> observer7 = null;
			LiveObserver<IValueState<T8>> observer8 = null;
			LiveObserver<IValueState<T9>> observer9 = null;
			LiveObserver<IValueState<T10>> observer10 = null;
			LiveObserver<IValueState<T11>> observer11 = null;
			LiveObserver<IValueState<T12>> observer12 = null;
			LiveObserver<IValueState<T13>> observer13 = null;
			LiveObserver<IValueState<T14>> observer14 = null;
			LiveObserver<IValueState<T15>> observer15 = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged =>
                {
					source0.Subscribe(observer0 = source0.CreateObserver(innerChanged));
					source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
					source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
					source3.Subscribe(observer3 = source3.CreateObserver(innerChanged));
					source4.Subscribe(observer4 = source4.CreateObserver(innerChanged));
					source5.Subscribe(observer5 = source5.CreateObserver(innerChanged));
					source6.Subscribe(observer6 = source6.CreateObserver(innerChanged));
					source7.Subscribe(observer7 = source7.CreateObserver(innerChanged));
					source8.Subscribe(observer8 = source8.CreateObserver(innerChanged));
					source9.Subscribe(observer9 = source9.CreateObserver(innerChanged));
					source10.Subscribe(observer10 = source10.CreateObserver(innerChanged));
					source11.Subscribe(observer11 = source11.CreateObserver(innerChanged));
					source12.Subscribe(observer12 = source12.CreateObserver(innerChanged));
					source13.Subscribe(observer13 = source13.CreateObserver(innerChanged));
					source14.Subscribe(observer14 = source14.CreateObserver(innerChanged));
					source15.Subscribe(observer15 = source15.CreateObserver(innerChanged));
                },
				() =>
				{
					observer0.GetNotify();
					observer1.GetNotify();
					observer2.GetNotify();
					observer3.GetNotify();
					observer4.GetNotify();
					observer5.GetNotify();
					observer6.GetNotify();
					observer7.GetNotify();
					observer8.GetNotify();
					observer9.GetNotify();
					observer10.GetNotify();
					observer11.GetNotify();
					observer12.GetNotify();
					observer13.GetNotify();
					observer14.GetNotify();
					observer15.GetNotify();
				},
                (innerChanged, oldState) =>
				{
					// update observers
					var state0 = observer0.GetState();
					var state1 = observer1.GetState();
					var state2 = observer2.GetState();
					var state3 = observer3.GetState();
					var state4 = observer4.GetState();
					var state5 = observer5.GetState();
					var state6 = observer6.GetState();
					var state7 = observer7.GetState();
					var state8 = observer8.GetState();
					var state9 = observer9.GetState();
					var state10 = observer10.GetState();
					var state11 = observer11.GetState();
					var state12 = observer12.GetState();
					var state13 = observer13.GetState();
					var state14 = observer14.GetState();
					var state15 = observer15.GetState();

					// work out expression result
					var newState = new ValueState<TResult>
						{
							Status = state0.Status.And(state1.Status.And(state2.Status.And(state3.Status.And(state4.Status.And(state5.Status.And(state6.Status.And(state7.Status.And(state8.Status.And(state9.Status.And(state10.Status.And(state11.Status.And(state12.Status.And(state13.Status.And(state14.Status.And(state15.Status))))))))))))))),
							LastUpdated = Math.Max(state0.LastUpdated, Math.Max(state1.LastUpdated, Math.Max(state2.LastUpdated, Math.Max(state3.LastUpdated, Math.Max(state4.LastUpdated, Math.Max(state5.LastUpdated, Math.Max(state6.LastUpdated, Math.Max(state7.LastUpdated, Math.Max(state8.LastUpdated, Math.Max(state9.LastUpdated, Math.Max(state10.LastUpdated, Math.Max(state11.LastUpdated, Math.Max(state12.LastUpdated, Math.Max(state13.LastUpdated, Math.Max(state14.LastUpdated, state15.LastUpdated))))))))))))))),
						};
					newState.NewValue = newState.Status.IsConnected()
						? func(state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13, state14, state15)
						: default(TResult);
					return newState;
				},
				() =>
				{
					observer0.Dispose();
					observer1.Dispose();
					observer2.Dispose();
					observer3.Dispose();
					observer4.Dispose();
					observer5.Dispose();
					observer6.Dispose();
					observer7.Dispose();
					observer8.Dispose();
					observer9.Dispose();
					observer10.Dispose();
					observer11.Dispose();
					observer12.Dispose();
					observer13.Dispose();
					observer14.Dispose();
					observer15.Dispose();
				});
		}

		public static ILiveValue<TResult> LiveInvoke<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> func, ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14, ILiveValue<T15> source15)
        {
            return
                new Func<IValueState<T0>, IValueState<T1>, IValueState<T2>, IValueState<T3>, IValueState<T4>, IValueState<T5>, IValueState<T6>, IValueState<T7>, IValueState<T8>, IValueState<T9>, IValueState<T10>, IValueState<T11>, IValueState<T12>, IValueState<T13>, IValueState<T14>, IValueState<T15>, TResult>((state0, state1, state2, state3, state4, state5, state6, state7, state8, state9, state10, state11, state12, state13, state14, state15) => func(state0.NewValue, state1.NewValue, state2.NewValue, state3.NewValue, state4.NewValue, state5.NewValue, state6.NewValue, state7.NewValue, state8.NewValue, state9.NewValue, state10.NewValue, state11.NewValue, state12.NewValue, state13.NewValue, state14.NewValue, state15.NewValue))
                    .LiveInvoke(source0,source1,source2,source3,source4,source5,source6,source7,source8,source9,source10,source11,source12,source13,source14,source15);
        }

        public static LiveFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> Create<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15, TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> func)
        {
            return (t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15) => func.LiveInvoke(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

		public static ILiveValue<TResult> Join<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15, TResult>(this ILiveValue<T0> source0, ILiveValue<T1> source1, ILiveValue<T2> source2, ILiveValue<T3> source3, ILiveValue<T4> source4, ILiveValue<T5> source5, ILiveValue<T6> source6, ILiveValue<T7> source7, ILiveValue<T8> source8, ILiveValue<T9> source9, ILiveValue<T10> source10, ILiveValue<T11> source11, ILiveValue<T12> source12, ILiveValue<T13> source13, ILiveValue<T14> source14, ILiveValue<T15> source15, Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> selector)
        {
            return selector.Create()(source0, source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14, source15);
        }
    }
    
}

