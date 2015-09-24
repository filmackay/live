using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveDictionary<TKey, TValue> Unwrap<TKey, TValue>(this ILiveDictionary<TKey, ILiveValue<TValue>> source)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, ILiveValue<TValue>>, IDictionaryDelta<TKey, ILiveValue<TValue>>>> observer = null;
            var valueObservers = new Dictionary<TKey, LiveObserver<IValueState<TValue>>>();
            var cache = new CollectionStateCache<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>, IDictionaryDelta<TKey, TValue>>(new Dictionary<TKey, TValue>());
            var notifyValuesChanged = new NotifyList<KeyValuePair<TKey, LiveObserver<IValueState<TValue>>>>();

            return LiveDictionaryObservable<TKey, TValue>.Create(
                innerChanged =>
                {
                    notifyValuesChanged.OnNotify = innerChanged;
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        // get state
                        using (var state = observer.GetState())
                        {
                            var lastUpdated = state.LastUpdated;
                            DictionaryDelta<TKey, TValue> delta = null;

                            notifyValuesChanged.ProcessGet(getValues =>
                            {
                                if (state.Status.IsDisconnecting())
                                {
                                    // clear all items
                                    valueObservers.Values.ForEach(s => s.Dispose());
                                    valueObservers.Clear();
                                    cache.Cache.Clear();
                                }

                                if (state.Status.IsConnecting())
                                {
                                    // subscribe to all items
                                    valueObservers.AddRange(state.Inner.Select(kv =>
                                    {
                                        LiveObserver<IValueState<TValue>> obs = null;
                                        return Callback
                                            .Split(
                                                () =>
                                                {
                                                    var itemState = obs.GetState();
                                                    lastUpdated = Math.Max(lastUpdated, itemState.LastUpdated);
                                                },
                                                t => notifyValuesChanged.Add(t),
                                                callback =>
                                                {
                                                    kv.Value.Subscribe(obs = kv.Value.CreateObserver(callback));
                                                    return KeyValuePair.Create(kv.Key, obs);
                                                });
                                    }));
                                }

                                if (state.Status == StateStatus.Connected && state.Delta != null)
                                {
                                    // process delta - un/subscribe to items
                                    if (state.Delta.Deletes != null)
                                        foreach (var delete in state.Delta.Deletes)
                                            valueObservers[delete.Key].Dispose();
                                    if (state.Delta.Inserts != null)
                                        valueObservers.AddRange(
                                            state.Delta.Inserts.Select(
                                                add =>
                                                    Callback
                                                        .Split<KeyValuePair<TKey, LiveObserver<IValueState<TValue>>>>(
                                                            (kv, inside) => notifyValuesChanged.Add(kv, !inside),
                                                            callback =>
                                                            {
                                                                var kv = default(KeyValuePair<TKey, LiveObserver<IValueState<TValue>>>);
                                                                kv = KeyValuePair.Create(add.Key, add.Value.CreateObserver(() => callback(kv)));
                                                                add.Value.Subscribe(kv.Value);
                                                                return kv;
                                                            }
                                                        )
                                                )
                                            );
                                }

                                // process list of value subscriptions that have changed
                                var valuesChanged = getValues();
                                if (state.Status == StateStatus.Connected && valuesChanged != null)
                                {
                                    delta = new DictionaryDelta<TKey, TValue>();
                                    // create delta from item changes
                                    foreach (var t in valuesChanged)
                                    {
                                        // get copies of delta - no need for stability
                                        var key = t.Key;
                                        var obs = t.Value;
                                        var valueState = obs.GetState();
                                        lastUpdated = Math.Max(lastUpdated, valueState.LastUpdated);

                                        if (valueState.Status == StateStatus.Connecting)
                                            delta.Insert(-1, new[] { KeyValuePair.Create(key, valueState.NewValue) });
                                        else if (valueState.Status == StateStatus.Disconnecting || valueState.Status == StateStatus.Completing)
                                        {
                                            // take item out of results
                                            delta.Delete(-1, new[] { KeyValuePair.Create(key, valueState.OldValue) });

                                            if (valueState.Status == StateStatus.Completing)
                                            {
                                                // remove subscription entirely
                                                valueObservers.Remove(key);
                                            }
                                        }
                                        else
                                            delta.Update(-1, KeyValuePair.Create(key, valueState.OldValue), KeyValuePair.Create(key, valueState.NewValue));
                                    }
                                }
                            });

                            // apply changes to cache
                            cache.AddState
                                (state.Status,
                                valueObservers
                                    .Where(kv => kv.Value.Last.Status.IsConnected())
                                    .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Last.NewValue)),
                                delta,
                                lastUpdated,
                                true);
                        }
                    }

                    stateLock.DowngradeToReader();
                    return cache.Copy(stateLock);
                },
                () => observer.Dispose());
        }

        public static ILiveDictionary<TKey, TValue> Unwrap<TKey, TValue, TILiveValue>(this ILiveDictionary<TKey, TILiveValue> source)
            where TILiveValue : ILiveValue<TValue>
        {
            return source
                .SelectDictionaryStatic(kv => (ILiveValue<TValue>)kv.Value)
                .Unwrap();
        }
    }
}
