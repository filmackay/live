using System;
using System.Reactive.Linq;


namespace Vertigo.Live
{
    public static partial class LiveObservable
    {
        public static ILiveValue<TimeSpan?> ToLatency<TIState>(this ILiveObservable<TIState> source)
            where TIState : IState
        {
            var observer = default(LiveObserver<TIState>);

            return LiveValueObservable<TimeSpan?>.Create(
                innerChanged => source.Subscribe(observer = source.Subscribe(o => innerChanged())),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    var now = HiResTimer.Now();
                    var newState = observer.GetState();
                    var ret = new ValueState<TimeSpan?>
                    {
                        Status = StateStatus.Connected,
                        LastUpdated = now,
                        NewValue = HiResTimer.ToTimeSpan(now - newState.LastUpdated),
                    };
                    return ret;
                },
                () => observer.Dispose());
        }
    }
}
