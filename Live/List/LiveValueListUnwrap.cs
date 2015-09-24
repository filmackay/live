using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveList<T> Unwrap<T>(this ILiveValue<ILiveList<T>> source)
        {
            LiveObserver<IValueState<ILiveList<T>>> outerObserver = null;
            LiveObserver<ICollectionState<T, IListDelta<T>>> innerObserver = null;

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    source.Subscribe(outerObserver = source.CreateObserver(innerChanged));
                    return Lockers.Empty;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        var outerState = outerObserver.GetState();

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
                                    callback =>
                                        outerState
                                            .NewValue
                                            .Subscribe(innerObserver = outerState.NewValue.CreateObserver(() => callback())));
                        }
                    }

                    // Disconnect if we have no outer to subscribe to, or return empty result if there is no inner
                    if (innerObserver == null)
                        return new CollectionState<T, IListDelta<T>, IList<T>>();

                    // translate inner
                    var ret = innerObserver
                        .GetState()
                        .Extract(
                            true, 
                            (inner, delta) => delta.ToListDelta(item => item),
                            inner => inner);
                    var r = ret.Inner.ToArray();
                    return ret;
                },
                () => outerObserver.Dispose());
        }
    }
}
