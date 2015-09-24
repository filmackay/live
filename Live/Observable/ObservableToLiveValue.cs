using System;


namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> ToLiveValue<T>(this IObservable<T> source)
        {
            var lastState = (IValueState<T>)new ValueState<T>();
            var subscription = default(IDisposable);
            return LiveValueObservable<T>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(i =>
                        {
                            // new value
                            lastState =
                                new ValueState<T>
                                {
                                    LastUpdated = HiResTimer.Now(),
                                    Status = StateStatus.Connected,
                                    NewValue = i,
                                };
                            innerChanged();
                        },
                        () =>
                        {
                            // completed
                            lastState =
                                new ValueState<T>
                                {
                                    LastUpdated = HiResTimer.Now(),
                                    Status = StateStatus.Completing,
                                };
                            innerChanged();
                        });
                },
                () => { },
                (innerChanged, oldState) => oldState.Add(lastState),
                () => subscription.Dispose());
        }

        public static ILiveValue<T> ToLiveValue<T>(this IObservable<IValueState<T>> source)
        {
            var lastState = (IValueState<T>)new ValueState<T>();
            var subscription = default (IDisposable);
            return LiveValueObservable<T>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(newState =>
                        {
                            // new value
                            lastState = lastState.Add(newState);
                            innerChanged();
                        });
                },
                () => { },
                (innerChanged, oldState) => oldState.Add(lastState),
                () => subscription.Dispose());
        }
    }
}
