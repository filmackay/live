using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<T> Unwrap<T, TIDelta>(this ILiveCollection<ILiveValue<T>, TIDelta> source)
            where TIDelta : ICollectionDelta<ILiveValue<T>>
        {
            LiveObserver<ICollectionState<ILiveValue<T>, TIDelta>> outerObserver = null;
            var innerObservers = new MultiMap<ILiveValue<T>, LiveObserver<IValueState<T>>>();
            var cache = new CollectionStateCache<T, ICollection<T>, ICollectionDelta<T>>(new Collection<T>());
            NotifyList<LiveObserver<IValueState<T>>> valuesChanged = null;

            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    valuesChanged = new NotifyList<LiveObserver<IValueState<T>>> { OnNotify = innerChanged };
                    source.Subscribe(outerObserver = source.CreateObserver(innerChanged));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        // get state
                        using (var outerState = outerObserver.GetState())
                        {
                            var lastUpdated = outerState.LastUpdated;
                            CollectionDelta<T> delta = null;

                            valuesChanged.ProcessGet(getChanges =>
                            {
                                if (outerState.Status.IsDisconnecting())
                                {
                                    // clear all items
                                    innerObservers.Values.ForEach(s => s.Dispose());
                                    innerObservers.Clear();
                                    cache.Cache.Clear();
                                }

                                if (outerState.Status.IsConnecting())
                                {
                                    // subscribe to all items
                                    innerObservers.AddRange(
                                        outerState.Inner.Select(
                                            item =>
                                                KeyValuePair.Create(
                                                    item,
                                                    Callback.Split<LiveObserver<IValueState<T>>>(
                                                        o =>
                                                        {
                                                            var innerState = o.GetState();
                                                            lastUpdated = Math.Max(lastUpdated, innerState.LastUpdated);
                                                        },
                                                        o => valuesChanged.Add(o),
                                                        callback => item.Subscribe(callback))
                                                    )
                                                )
                                    );
                                }

                                if (outerState.Status == StateStatus.Connected && outerState.Delta.HasChange())
                                {
                                    // process delta - un/subscribe to items
                                    if (outerState.Delta.Deletes != null)
                                        foreach (var delete in outerState.Delta.Deletes)
                                            innerObservers[delete].Dispose();
                                    if (outerState.Delta.Inserts != null)
                                        innerObservers.AddRange(
                                            outerState.Delta.Inserts.Select(
                                                add =>
                                                    KeyValuePair.Create(
                                                        add,
                                                        Callback.Split<LiveObserver<IValueState<T>>>(
                                                            (o, inside) => valuesChanged.Add(o, !inside),
                                                            callback => add.Subscribe(callback)))));
                                }

                                // get the values that have changed
                                var changes = getChanges();
                                if (outerState.Status == StateStatus.Connected && changes != null)
                                {
                                    // create delta from item changes
                                    delta = new CollectionDelta<T>();
                                    foreach (var itemObserver in changes)
                                    {
                                        // get copies of delta - no need for stability
                                        var valueState = itemObserver.GetState();
                                        lastUpdated = Math.Max(lastUpdated, valueState.LastUpdated);
                                        var item = itemObserver.Observable as ILiveValue<T>;

                                        // process delta
                                        if (valueState.Status == StateStatus.Connecting)
                                        {
                                            // new item
                                            delta.Insert(-1, new[] { valueState.NewValue });
                                            cache.Cache.Add(valueState.NewValue);
                                        }
                                        else if (valueState.Status == StateStatus.Completing || valueState.Status == StateStatus.Disconnecting)
                                        {
                                            delta.Delete(-1, new[] { valueState.OldValue });
                                            cache.Cache.Remove(valueState.OldValue);

                                            if (valueState.Status == StateStatus.Completing)
                                                innerObservers.Remove(item, itemObserver);
                                        }
                                        else
                                        {
                                            delta.Update(-1, valueState.OldValue, valueState.NewValue);
                                            cache.Cache.Remove(valueState.OldValue);
                                            cache.Cache.Add(valueState.NewValue);
                                        }
                                    }
                                }
                            });

                            // apply changes to cache
                            cache.AddState
                                (outerState.Status,
                                innerObservers
                                    .Values
                                    .Where(s => s.Last.Status.IsConnected())
                                    .Select(s => s.Last.NewValue),
                                delta,
                                lastUpdated,
                                false);
                        }
                    }
                    stateLock.DowngradeToReader();
                    return cache.Copy(stateLock);
                },
                () => outerObserver.Dispose());
        }
    }
}
