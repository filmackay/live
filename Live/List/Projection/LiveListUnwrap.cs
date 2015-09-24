using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Unwrap<T>(this ILiveList<ILiveValue<T>> source)
        {
            LiveObserver<ICollectionState<ILiveValue<T>, IListDelta<ILiveValue<T>>>> outerObserver = null;
            var innerObservers = new List<LiveObserver<IValueState<T>>>();
            var connectedInnerObservers = new VirtualList<LiveObserver<IValueState<T>>>(true);
            var cache = new CollectionStateCache<T, IList<T>, IListDelta<T>>(new List<T>());
            NotifyList<LiveObserver<IValueState<T>>> valuesChanged = null;

            Action ClearItems =
                () =>
                {
                    // clear all items
                    innerObservers.ForEach(s => s.Dispose());
                    innerObservers.Clear();
                    connectedInnerObservers.Clear();
                    cache.Cache.Clear();
                };

            return LiveListObservable<T>.Create(
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
                            ListDelta<T> delta = null;

                            valuesChanged.ProcessGet(getValues =>
                            {
                                if (outerState.Status.IsDisconnecting())
                                    ClearItems();

                                if (outerState.Status.IsConnecting())
                                {
                                    // subscribe to all items
                                    innerObservers.AddRange(
                                        outerState.Inner.Select((item, sourceIndex) =>
                                            {
                                                var itemObserver = item.CreateObserver(o => valuesChanged.Add(o));
                                                item.Subscribe(itemObserver);
                                                
                                                var itemState = itemObserver.GetState();
                                                lastUpdated = Math.Max(lastUpdated, itemState.LastUpdated);

                                                // make room for connected observer
                                                connectedInnerObservers.AdjustIndex(sourceIndex, 1);
                                                if (itemState.Status.IsConnected())
                                                    connectedInnerObservers.SetAt(sourceIndex, itemObserver);
                                                return itemObserver;
                                            }));
                                }

                                else if (outerState.Status == StateStatus.Connected && outerState.Delta != null)
                                {
                                    // process delta - un/subscribe to items
                                    outerState.Delta.IndexDeltas.ForEach(sourceIndex =>
                                    {
                                        // delete
                                        for (var i = 0; i < sourceIndex.Data.DeleteItems.Count(); i++)
                                            innerObservers[sourceIndex.Index].Dispose();

                                        // insert
                                        sourceIndex.Data.InsertItems.ForEach(item =>
                                        {
                                            var itemObserver = item.CreateObserver(o => valuesChanged.Add(o));
                                            innerObservers.Insert(sourceIndex.Index, itemObserver);
                                            connectedInnerObservers.AdjustIndex(sourceIndex.Index, +1);
                                            item.Subscribe(itemObserver);
                                        });
                                    });
                                }

                                // get the values that have changed
                                var changes = getValues();
                                if (outerState.Status == StateStatus.Connected && changes != null)
                                {
                                    // create delta from item changes
                                    delta = new ListDelta<T>();
                                    changes.ForEach(innerObserver =>
                                    {
                                        // get copies of delta - no need for stability
                                        var innerState = innerObserver.GetState();
                                        lastUpdated = Math.Max(lastUpdated, innerState.LastUpdated);

                                        // handle connections
                                        if (innerState.Status == StateStatus.Connecting)
                                        {
                                            // add to connected subscriptions
                                            var sourceIndex = innerObservers.IndexOf(innerObserver);
                                            var resultIndex = connectedInnerObservers.SetAt(sourceIndex, innerObserver).DenseIndex;

                                            // insert into delta and cache
                                            delta.Insert(resultIndex, new[] { innerState.NewValue });
                                            cache.Cache.Insert(resultIndex, innerState.NewValue);
                                        }
                                        else if (innerState.Status == StateStatus.Disconnecting || innerState.Status == StateStatus.Completing)
                                        {
                                            // find node
                                            var node = connectedInnerObservers.NodeOf(innerObserver);
                                            var sourceIndex = node.Index;
                                            var resultIndex = node.DenseIndex;

                                            // remove from connected subscriptions, delta and cache
                                            delta.Delete(resultIndex, new[] { innerState.OldValue });
                                            cache.Cache.RemoveAt(resultIndex);

                                            // end subscription
                                            innerObservers.RemoveAt(sourceIndex);
                                            connectedInnerObservers.RemoveAt(sourceIndex);
                                        }
                                        else
                                        {
                                            // adjust in delta and cache
                                            var resultIndex = connectedInnerObservers.NodeOf(innerObserver).DenseIndex;
                                            delta.Update(resultIndex, innerState.OldValue, innerState.NewValue);
                                            cache.Cache[resultIndex] = innerState.NewValue;
                                        }
                                    });
                                }
                            });

                            // apply changes to cache
                            cache.AddState
                                (outerState.Status,
                                innerObservers.Where(s => s.Last.Status.IsConnected()).Select(s => s.Last.NewValue),
                                delta,
                                lastUpdated,
                                false);
                        }
                    }
                    stateLock.DowngradeToReader();

                    return cache.Copy(stateLock);
                },
                () =>
                {
                    outerObserver.Dispose();
                    ClearItems();
                });
        }
    }
}
