using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<T> SelectMany<T, TOuterIDelta, TInnerIDelta>(this ILiveCollection<ILiveCollection<T, TInnerIDelta>, TOuterIDelta> source)
            where TOuterIDelta : class, ICollectionDelta<ILiveCollection<T, TInnerIDelta>>
            where TInnerIDelta : class, ICollectionDelta<T>
        {
            NotifyList<Tuple<LiveObserver<ICollectionState<T, TInnerIDelta>>, ICollection<T>>> itemsChanged = null;
            LiveObserver<ICollectionState<ILiveCollection<T, TInnerIDelta>, TOuterIDelta>> outerObserver = null;
            var innerItems = new MultiMap<ILiveCollection<T, TInnerIDelta>, Tuple<LiveObserver<ICollectionState<T, TInnerIDelta>>, ICollection<T>>>();
            var cache = new CollectionStateCache<T, ICollection<T>, ICollectionDelta<T>>(new Collection<T>());

            var SubscribeToItem = new Action<ILiveCollection<T, TInnerIDelta>>(
                item =>
                {
                    // subscribe to an idependent version of the inner collection
                    Tuple<LiveObserver<ICollectionState<T, TInnerIDelta>>, ICollection<T>> t = null;
                    var observer = item.CreateObserver(() => itemsChanged.Add(t));
                    t = Tuple.Create(observer, new Collection<T>() as ICollection<T>);
                    innerItems.Add(item, t);
                    item.Subscribe(observer);
                });

            var UnsubscribeToItem = new Action<ILiveCollection<T, TInnerIDelta>>(
                item =>
                {
                    var t = innerItems[item];
                    t.Item1.Dispose();
                });

            var DisposeInnerItems = new Action(
                () =>
                {
                    // dispose all subscriptions (no notification)
                    innerItems.Values.ForEach(t => t.Item1.Dispose());
                    innerItems.Clear();
                    cache.Cache.Clear();
                });

            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    itemsChanged = new NotifyList<Tuple<LiveObserver<ICollectionState<T, TInnerIDelta>>, ICollection<T>>> { OnNotify = () => innerChanged() };
                    source.Subscribe(outerObserver = source.CreateObserver(() => innerChanged()));
                    return cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        // get state
                        using (var state = outerObserver.GetState())
                        {
                            var lastUpdated = state.LastUpdated;
                            itemsChanged.ProcessGet(getItemsChanged =>
                            {
                                if (state.Status.IsDisconnecting())
                                    DisposeInnerItems();
                                else if (state.Status.IsConnecting())
                                    // subscribe to all
                                    state.Inner.ForEach(SubscribeToItem);
                                else if (state.Delta.HasChange())
                                {
                                    // un/subscribe to changed items
                                    if (state.Delta.Inserts != null)
                                        state.Delta.Inserts.ForEach(SubscribeToItem);
                                    if (state.Delta.Deletes != null)
                                        state.Delta.Deletes.ForEach(UnsubscribeToItem);
                                }

                                // process changed inner collections
                                var changes = getItemsChanged();
                                CollectionDelta<T> delta = null;
                                if (changes != null)
                                {
                                    delta = new CollectionDelta<T>();
                                    foreach (var t in changes)
                                    {
                                        var innerObserver = t.Item1;
                                        var innerCache = t.Item2;

                                        using (var innerState = innerObserver.GetState())
                                        {
                                            lastUpdated = Math.Max(lastUpdated, innerState.LastUpdated);

                                            // process inner item delta
                                            if (innerState.Status.IsDisconnecting())
                                            {
                                                // remove any existing items
                                                delta.Delete(-1, innerCache);
                                                innerCache.Clear();
                                            }
                                            if (innerState.Status.IsConnecting())
                                            {
                                                // add all items
                                                innerCache.AddRange(innerState.Inner);
                                                delta.Insert(-1, innerCache);
                                            }
                                            if (innerState.Status == StateStatus.Completing)
                                            {
                                                // remove subscription
                                                innerItems.Remove(t.Item1.Observable as ILiveCollection<T, TInnerIDelta>, t);
                                            }
                                            else if (innerState.Delta.HasChange())
                                            {
                                                // apply inner delta
                                                if (innerState.Delta.Inserts != null)
                                                {
                                                    delta.Insert(-1, innerState.Delta.Inserts);
                                                    innerCache.AddRange(innerState.Delta.Inserts);
                                                }
                                                if (innerState.Delta.Deletes != null)
                                                {
                                                    delta.Delete(-1, innerState.Delta.Deletes);
                                                    innerCache.RemoveRange(innerState.Delta.Deletes);
                                                }
                                            }
                                        }
                                    }
                                }

                                // update cache
                                cache.AddState(
                                    state.Status,
                                    innerItems.Values.Select(t => t.Item2).SelectMany(i => i),
                                    delta,
                                    lastUpdated,
                                    true);
                            });
                        }
                    }
                    stateLock.DowngradeToReader();

                    // return copy of cache
                    return cache.Copy(stateLock);
                },
                () =>
                {
                    outerObserver.Dispose();
                    DisposeInnerItems();
                });
        }

        public static ILiveCollection<TInner> SelectMany<TOuter, TOuterIDelta, TInner, TInnerIDelta>(this ILiveCollection<TOuter, TOuterIDelta> source, Func<TOuter, ILiveCollection<TInner, TInnerIDelta>> selector)
            where TOuterIDelta : class, ICollectionDelta<TOuter>
            where TInnerIDelta : class, ICollectionDelta<TInner>
        {
            return source
                .SelectStatic(selector)
                .SelectMany();
        }

        public static IObservable<T> Unwrap<T, TIDelta>(this ILiveCollection<IObservable<T>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<IObservable<T>>
        {
            return
                Observable.Create<T>(observer =>
                {
                    var map = new MultiMap<IObservable<T>, IDisposable>();
                    var subject = new Subject<T>();
                    subject.Subscribe(observer);

                    return source
                        .States()
                        .Subscribe(state =>
                        {
                            if (state.Status.IsDisconnecting())
                            {
                                map.ForEach(kv => kv.Value.Dispose());
                                map.Clear();
                            }
                            if (state.Status.IsConnecting())
                            {
                                map.AddRange(state.Inner.Select(observable => new KeyValuePair<IObservable<T>, IDisposable>(observable, observable.Subscribe(subject))));
                            }
                            else if (state.Delta != null)
                            {
                                if (state.Delta.Inserts != null)
                                    map.AddRange(state.Delta.Inserts.Select(observable => new KeyValuePair<IObservable<T>, IDisposable>(observable, observable.Subscribe(subject))));
                                if (state.Delta.Deletes != null)
                                    map.RemoveRange(state.Delta.Deletes.Select(observable =>
                                        {
                                            var kv = new KeyValuePair<IObservable<T>, IDisposable>(observable, map[observable]);
                                            kv.Value.Dispose();
                                            return kv;
                                        }));
                            }
                        });
                });
        }

        public static IObservable<TInner> SelectMany<TOuter, TOuterIDelta, TInner>(this ILiveCollection<TOuter, TOuterIDelta> source, Func<TOuter, IObservable<TInner>> selector)
            where TOuterIDelta : class, ICollectionDelta<TOuter>
        {
            return source
                .SelectStatic(selector)
                .Unwrap();
        }
    }
}
