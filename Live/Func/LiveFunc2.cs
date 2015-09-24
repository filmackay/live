using System;


namespace Vertigo.Live
{
    public delegate ILiveValue<TResult> LiveFunc<out TResult>();

    public static partial class LiveFunc
    {
        public static ILiveValue<TResult> Invoke<TResult>(this Func<TResult> func)
        {
            return LiveValueObservable<TResult>.Create(
                innerChanged => innerChanged(),
                () => { },
                (innerChanged, oldState) =>
                {
                    innerChanged();
                    return oldState.Add(new ValueState<TResult>
                    {
                        Status = StateStatus.Connected,
                        LastUpdated = HiResTimer.Now(),
                        NewValue = func(),
                    });
                },
                () => { });
        }
    }

    public delegate ILiveValue<TResult> LiveFunc<in T0, out TResult>(ILiveValue<T0> t0);

    public static partial class LiveFunc
    {
        public static ILiveValue<TResult> LiveInvoke<T0, TResult>(this Func<IValueState<T0>, TResult> func, ILiveValue<T0> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            LiveObserver<IValueState<T0>> observer = null;

            return LiveValueObservable<TResult>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    // update observers
                    var state = observer.GetState();

                    // work out expression result
                    var newValue = default(TResult);
                    if (state.Status.IsConnected())
                        newValue = func(state);
                    return new ValueState<TResult>
                    {
                        Status = state.Status,
                        LastUpdated = state.LastUpdated,
                        NewValue = newValue
                    };
                },
                () => observer.Terminate());
        }

        public static ILiveValue<TResult> LiveInvoke<T0, TResult>(this Func<T0, TResult> func, ILiveValue<T0> source0)
        {
            return
                new Func<IValueState<T0>, TResult>(
                    state0 => func(state0.NewValue)
                    //state0 =>
                    //{
                    //    Console.WriteLine("#{0} {1}.{2}({3} : {4})",
                    //        Thread.CurrentThread.ManagedThreadId,
                    //        func.Method.DeclaringType == null ? "null" : func.Method.DeclaringType.Name,
                    //        func.Method.Name,
                    //        state0.NewValue == null ? "null" : state0.NewValue.ToString(),
                    //        typeof(T0));
                    //    if (func.Method.Name == "<OnCreated>b__29")
                    //        Debug.Print("Dodgy");
                    //    return func(state0.NewValue);
                    //}
                    )
                    .LiveInvoke(source0);
        }

        public static LiveFunc<TSource, TResult> Create<TSource, TResult>(Func<TSource, TResult> func)
        {
            return t => func.LiveInvoke(t);
        }
    }
}
