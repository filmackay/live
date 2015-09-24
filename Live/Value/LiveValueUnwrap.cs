using System;
using System.Reactive.Linq;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> Unwrap<T>(this ILiveValue<ILiveValue<T>> source)
        {
            LiveObserver<IValueState<ILiveValue<T>>> outerObserver = null;
            LiveObserver<IValueState<T>> innerObserver = null;

            return LiveValueObservable<T>.Create(
                innerChanged => source.Subscribe(outerObserver = source.CreateObserver(innerChanged)),
                () =>
                {
                    outerObserver.GetNotify();
                    if (innerObserver != null)
                        innerObserver.GetNotify();
                },
                (innerChanged, oldState) =>
                {
                    var outerState = outerObserver.GetState();

                    // unsubscribe to old value
                    if (innerObserver != null && outerState.HasEffect())
                    {
                        innerObserver.Terminate();
                        innerObserver = null;
                    }

                    // subscribe to new value
                    var result = new ValueState<T>();
                    if (outerState.HasEffect() && outerState.NewValue != null)
                    {
                        Callback
                            .SuppressInside(
                                innerChanged,
                                callback => outerState.NewValue.Subscribe(innerObserver = outerState.NewValue.CreateObserver(callback)));
                    }

                    // get inner
                    if (innerObserver != null)
                    {
                        var innerState = innerObserver.GetState();
                        result.NewValue = innerState.NewValue;
                        result.Status = innerState.Status;
                        result.LastUpdated = Math.Max(outerState.LastUpdated, innerState.LastUpdated);
                    }
                    else
                        result.LastUpdated = 0;
                    return result;
                },
                () =>
                {
                    outerObserver.Terminate();
                    if (innerObserver != null)
                        innerObserver.Terminate();
                });
        }

        public static IObservable<T> Unwrap<T>(this ILiveValue<IObservable<T>> source)
        {
            return source
                .Values()
                .Switch();
        }
    }
}
