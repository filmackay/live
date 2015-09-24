using System;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> NonStop<T>(this ILiveValue<T> source, T @default = default(T))
        {
            return source
                .InterpretState(state =>
                    state.Add(
                        new ValueState<T>
                        {
                            Status = StateStatus.Connected,
                            NewValue = state.Status.IsInnerRelevant() ? state.NewValue : @default,
                            LastUpdated = state.LastUpdated,
                        })
                    );
        }
    }
}
