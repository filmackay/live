using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;


namespace Vertigo.Live
{
    public interface IValueState<out T> : IState
    {
        T NewValue { get; }
        T OldValue { get; }
    }

    public static class FussyEqualityComparer<T>
    {
        static FussyEqualityComparer()
        {
            if (typeof(T).IsValueType)
                Equals = (a, b) => StructuralComparisons.StructuralEqualityComparer.Equals(a, b);
            else
                // reference type
                Equals = (a, b) => ReferenceEquals(a, b);
        }

        new public static Func<T, T, bool> Equals;
    }

    public class ValueState<T> : State<IValueState<T>, ValueState<T>>, IValueState<T>
    {
        public T NewValue;
        public T OldValue;

        T IValueState<T>.NewValue
        {
            get { return NewValue; }
        }

        T IValueState<T>.OldValue
        {
            get { return OldValue; }
        }

        public override ValueState<T> Copy(bool detachStateLock)
        {
            var copy = base.Copy(detachStateLock);
            copy.OldValue = OldValue;
            copy.NewValue = NewValue;
            return copy;
        }

        public override bool HasChange
        {
            get { return Status.IsDeltaRelevant() && !FussyEqualityComparer<T>.Equals(OldValue, NewValue); }
        }

        public override StateStatus Add(StateStatus left, StateStatus right)
        {
            return left.AddSimple(right);
        }

        public override bool AddInline(IValueState<T> @new)
        {
            var changed = base.AddInline(@new);

            // update value
            if (!FussyEqualityComparer<T>.Equals(@new.NewValue, NewValue))
            {
                NewValue = @new.NewValue;
                changed = true;
            }

            return changed;
        }

        public override void NextInline()
        {
            base.NextInline();
            OldValue = NewValue;
        }

        public override string ToString()
        {
            if (Status == StateStatus.Connected)
            {
                return HasChange
                    ? string.Format("{0} => {1}", OldValue, NewValue)
                    : string.Format("{0}", NewValue);
            }
            return string.Format("{0}:{1}", Status, NewValue);
        }
    }

    public static partial class Extensions
    {
        public static bool HasNewValue<T>(this IValueState<T> state, IEqualityComparer<T> equalityComparer = null)
        {
            equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            switch (state.Status)
            {
                case StateStatus.Connecting:
                    return true;

                case StateStatus.Connected:
                case StateStatus.Reconnecting:
                case StateStatus.Disconnecting:
                case StateStatus.Completing:
                    return !equalityComparer.Equals(state.OldValue, state.NewValue);

                case StateStatus.Disconnected:
                case StateStatus.Completed:
                    break;
            }
            return false;
        }

        public static IValueState<T> Add<T>(this IValueState<T> old, IValueState<T> @new)
        {
            return new ValueState<T>
            {
                NewValue = @new.NewValue,
                OldValue = old == null ? default(T) : old.NewValue,
                Status = old.GetStatus().AddSimple(@new.Status),
                LastUpdated = old == null ? @new.LastUpdated : Math.Max(@new.LastUpdated, old.LastUpdated),
            };
        }

        public static IValueState<T> UpdateLastUpdated<T>(this IValueState<T> state)
        {
            return new ValueState<T>
            {
                NewValue = state.NewValue,
                OldValue = state.OldValue,
                Status = state.Status,
                LastUpdated = HiResTimer.Now(),
            };
        }

        public static IValueState<T> Next<T>(this IValueState<T> state)
        {
            return new ValueState<T>
            {
                LastUpdated = state.LastUpdated,
                NewValue = state.NewValue,
                OldValue = state.NewValue,
                Status = state.Status.Next(),
            };
        }

        public static IObservable<T> Values<T>(this ILiveObservable<IValueState<T>> source)
        {
            return
                source
                    .States()
                    .Where(state => state.Status.IsInnerRelevant())
                    .Select(state => state.NewValue);
        }

        public static IObservable<T> NewValues<T>(this ILiveObservable<IValueState<T>> source)
        {
            return
                source
                    .States()
                    .Where(state => state.Status.IsDeltaRelevant())
                    .Select(state => state.NewValue);
        }

        public static IObservable<TimeInterval<IValueState<T>>> ToTimeInterval<T>(this IObservable<IValueState<T>> source)
        {
            var prevLastUpdated = 0L;
            return
                source.Select(state =>
                    {
                        // work out interval since last item
                        var interval =
                            prevLastUpdated == 0
                            ? TimeSpan.Zero
                            : HiResTimer.ToTimeSpan(state.LastUpdated - prevLastUpdated);

                        // update previous
                        prevLastUpdated = state.LastUpdated;

                        // create
                        return new TimeInterval<IValueState<T>>(state, interval);
                    });
        }

        //public static IObservable<TimeInterval<T>> TimedValues<T>(this ILiveObservable<IValueState<T>> source)
        //{
        //    return
        //        source
        //            .States()
        //            .Where(state => state.Status.IsConnected())
        //            .ToTimeInterval();
        //}
    }
}
