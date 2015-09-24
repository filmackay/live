using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveDictionary<TKey,TValue> Unwrap<TKey,TValue>(this ILiveValue<ILiveDictionary<TKey,TValue>> source)
        {
            LiveObserver<IValueState<ILiveDictionary<TKey,TValue>>> outerObserver = null;
            LiveObserver<ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey,TValue>>> innerObserver = null;

            return LiveDictionaryObservable<TKey, TValue>.Create(
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

                    // stop if we have no outer to subscribe to, or return empty result if there is no inner
                    if (innerObserver == null)
                    {
                        var ret = new CollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>, IDictionary<TKey, TValue>>();
                        return ret;
                    }

                    // translate inner
                    return innerObserver
                        .GetState()
                        .Extract(
                            true,
                            (inner, delta) => delta.ToDictionaryDelta(item => item),
                            inner => inner);
                },
                () => outerObserver.Dispose());
        }
    }
}
