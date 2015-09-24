using System;

namespace Vertigo.Live
{
    public class LiveMutableValue<T> : LiveValue<T>, ILiveMutable<IValueState<T>>
    {
        private readonly ValueState<T> _state = new ValueState<T> { Status = StateStatus.Disconnected };

        public LiveMutableValue()
        {
        }

        public LiveMutableValue(T value)
        {
            this.Connect(value);
        }

        public bool Set(IValueState<T> state)
        {
            using (this.Lock())
            {
                var changed = _state.AddInline(state);
                if (changed)
                    InnerChanged();
                return changed;
            }
        }

        public override sealed void InnerGetValue(ref T value, ref StateStatus status, ref long lastUpdated)
        {
            using (this.Lock())
            {
                var state = _state.Extract(true);
                value = state.NewValue;
                status = state.Status;
                lastUpdated = _state.LastUpdated;
            }
        }
    }

    public static partial class Extensions
    {
        private static bool Set<T>(this ILiveMutable<IValueState<T>> live, StateStatus status, T value, long lastUpdated)
        {
            return live.Set(new ValueState<T> { Status = status, NewValue = value, LastUpdated = lastUpdated });
        }

        public static bool SetValue<T>(this ILiveMutable<IValueState<T>> live, T newValue, long lastUpdated)
        {
            return live.Set(StateStatus.Connected, newValue, lastUpdated);
        }

        public static bool SetValue<T>(this ILiveMutable<IValueState<T>> live, T newValue)
        {
            return live.Set(StateStatus.Connected, newValue, HiResTimer.Now());
        }

        public static void Connect<T>(this ILiveMutable<IValueState<T>> live, T newValue)
        {
            live.Set(StateStatus.Connecting, newValue, HiResTimer.Now());
        }

        public static void Connect<T>(this ILiveMutable<IValueState<T>> live, T newValue, long lastUpdated)
        {
            live.Set(StateStatus.Connecting, newValue, lastUpdated);
        }

        public static void Disconnect<T>(this ILiveMutable<IValueState<T>> live)
        {
            live.Set(StateStatus.Disconnecting, default(T), 0);
        }

        public static void Completed<T>(this ILiveMutable<IValueState<T>> live)
        {
            live.Set(StateStatus.Completing, default(T), 0);
        }
    }
}
