using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveCollection<T, TIDelta> Unwrap<T, TIDelta>(this ILiveValue<ILiveCollection<T, TIDelta>> source)
            where TIDelta : ICollectionDelta<T>
        {
            LiveObserver<IValueState<ILiveCollection<T, TIDelta>>> outerObserver = null;
            LiveObserver<ICollectionState<T, TIDelta>> innerObserver = null;

            return LiveCollectionObservable<T, TIDelta, ICollection<T>>.Create(
                innerChanged =>
                {
                    source.Subscribe(outerObserver = source.CreateObserver(innerChanged));
                    return outerObserver.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        var outerState = outerObserver.GetState(stateLock);

                        // unsubscribe to old value
                        if (innerObserver != null && (outerState == null || outerState.HasEffect()))
                        {
                            innerObserver.Dispose();
                            innerObserver = null;
                        }

                        if (outerState.Status.IsConnected() && outerState.HasEffect() && outerState.NewValue != null)
                        {
                            Debug.Assert(innerObserver == null);

                            // subscribe but do not notify
                            Callback
                                .SuppressInside(
                                    innerChanged,
                                    callback => outerState.NewValue.Subscribe(innerObserver = outerState.NewValue.CreateObserver(callback)));
                        }
                    }
                    else
                        using (stateLock) { }

                    // stop if we have no outer to subscribe to, or return empty result if there is no inner
                    if (innerObserver == null)
                    {
                        var ret = new CollectionState<T, TIDelta, ICollection<T>>();
                        return ret;
                    }

                    // translate inner
                    var state = innerObserver.GetState();
                    return state;
                },
                () => outerObserver.Dispose());
        }

        public static ILiveCollection<T> Unwrap<T>(this ILiveValue<ILiveCollection<T>> source)
        {
            LiveObserver<IValueState<ILiveCollection<T>>> outerObserver = null;
            LiveObserver<ICollectionState<T, ICollectionDelta<T>>> innerObserver = null;

            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(outerObserver = source.CreateObserver(() => innerChanged()));
                    return outerObserver.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        var outerState = outerObserver.GetState(stateLock);

                        // unsubscribe to old value
                        if (innerObserver != null && (outerState == null || outerState.HasEffect()))
                        {
                            innerObserver.Dispose();
                            innerObserver = null;
                        }

                        // subscribe to new inner
                        if (outerState.Status.IsConnected() && outerState.HasEffect() && outerState.NewValue != null)
                        {
                            Debug.Assert(innerObserver == null);

                            // subscribe but do not notify
                            Callback
                                .SuppressInside(
                                    innerChanged,
                                    callback => outerState.NewValue.Subscribe(innerObserver = outerState.NewValue.CreateObserver(callback)));
                        }
                    }

                    // disconnect if there is no inner
                    if (innerObserver == null)
                    {
                        var ret = new CollectionState<T, ICollectionDelta<T>, ICollection<T>>();
                        return ret;
                    }

                    // translate inner
                    return innerObserver
                        .GetState()
                        .Extract(
                            true,
                            (inner, delta) => delta,
                            inner => inner);
                },
                () => outerObserver.Dispose());
        }
    }
}
