using System;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> DisconnectOn<T>(this ILiveValue<T> source, Func<T, bool> filter)
        {
            return source
                .InterpretState(state =>
                    {
                        var hit = state.Status.IsInnerRelevant() && filter(state.NewValue);
                        if (!hit)
                            return state;
                        return
                            state.Add(new ValueState<T>
                                {
                                    Status = StateStatus.Disconnected,
                                    NewValue = state.NewValue,
                                    LastUpdated = state.LastUpdated,
                                });
                    });
        }

        public static ILiveValue<T> InterpretState<T>(this ILiveValue<T> source, Func<IValueState<T>, IValueState<T>> selector)
        {
            LiveObserver<IValueState<T>> observer = null;

            return LiveValueObservable<T>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    var state = observer.GetState();
                    state = oldState.Add(state);
                    return state;
                },
                () => observer.Dispose());
        }
    }
}
