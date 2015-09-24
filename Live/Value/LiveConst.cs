using System;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> ToLiveConst<T>(this T value)
        {
            return LiveValueObservable<T>.Create(
                innerChanged => innerChanged(),
                () => { },
                (innerChanged, oldState) => oldState.Add(new ValueState<T> { NewValue = value, Status = StateStatus.Connected }),
                () => { });
        }
    }
}
