using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public class LiveCollectionGroup<TKey, TElement, TIDelta> : LiveDictionaryView<TKey, ILiveCollection<TElement>>
        where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, TElement>>
    {
        public LiveCollectionGroup(ILiveCollection<KeyValuePair<TKey, TElement>, TIDelta> source)
        {
            _innerStateLocker = new Locker(_cache.WriteLock);

            _subscription = source.Subscribe(_observer = source.CreateObserver(InnerChanged));

            // subscribe to self, on behalf of groups
            _selfSubscription = Subscribe(_selfObserver = this.CreateObserver(InnerChanged));
        }

        private readonly CollectionStateCache<KeyValuePair<TKey, ILiveCollection<TElement>>, IDictionary<TKey, ILiveCollection<TElement>>, IDictionaryDelta<TKey, ILiveCollection<TElement>>> _cache = new CollectionStateCache<KeyValuePair<TKey, ILiveCollection<TElement>>, IDictionary<TKey, ILiveCollection<TElement>>, IDictionaryDelta<TKey, ILiveCollection<TElement>>>(new NullDictionary<TKey, ILiveCollection<TElement>>());
        private readonly IDisposable _subscription;
        private readonly IDisposable _selfSubscription;
        private readonly LiveObserver<ICollectionState<KeyValuePair<TKey, TElement>, TIDelta>> _observer;
        private readonly LiveObserver<ICollectionState<KeyValuePair<TKey, ILiveCollection<TElement>>, IDictionaryDelta<TKey, ILiveCollection<TElement>>>> _selfObserver;

        protected override Action PreInvalidateClients()
        {
            // lock groups
            var groups = _cache.Cache
                .Values
                .Cast<Group>();
            var groupsLock = groups
                .Select(g => (Locker)g.Lock)
                .ToArray()
                .Lock();

            // notify clients
            var postInvalidateGroups = groups.Select(g => g.PreInvalidateClients()).ToArray();
            var postInvalidateClients = base.PreInvalidateClients();

            return () =>
            {
                postInvalidateClients();
                postInvalidateGroups.ForEach();
                groupsLock.SafeDispose();
            };
        }

        private void UpdateState()
        {
            _selfObserver.GetState();
        }

        protected override ICollectionState<KeyValuePair<TKey, ILiveCollection<TElement>>, IDictionaryDelta<TKey, ILiveCollection<TElement>>> InnerGetState(bool notified, IDisposable stateLock)
        {
            if (notified)
            {
                // get state
                using (var state = _observer.GetState())
                {
                    // convert delta
                    DictionaryDelta<TKey, ILiveCollection<TElement>> delta = null;
                    if (state.Delta != null)
                    {
                        delta = new DictionaryDelta<TKey, ILiveCollection<TElement>>();

                        // handle deletes
                        if (state.Delta.Deletes != null)
                            foreach (var remove in state.Delta.Deletes)
                            {
                                var liveGrouping = _cache.Cache[remove.Key];
                                var group = liveGrouping as Group;

                                // remove from group
                                group.Remove(remove.Value, state.LastUpdated);

                                // remove group entirely if it is empty
                                if (group.Count == 0)
                                {
                                    // group is now empty - remove it
                                    delta.Delete(-1, new[] { KeyValuePair.Create(remove.Key, liveGrouping) });
                                    _cache.Cache.Remove(remove.Key);
                                }
                            }

                        // handle inserts
                        if (state.Delta.Inserts != null)
                            foreach (var add in state.Delta.Inserts)
                            {
                                ILiveCollection<TElement> liveGrouping;
                                if (!_cache.Cache.TryGetValue(add.Key, out liveGrouping))
                                {
                                    // create new group
                                    liveGrouping = new Group(this, new[] { add.Value }, state.LastUpdated);
                                    _cache.Cache.Add(add.Key, liveGrouping);
                                    delta.Insert(-1, new[] { KeyValuePair.Create(add.Key, liveGrouping) });
                                }
                                else
                                {
                                    // add to group
                                    (liveGrouping as Group).Add(add.Value, state.LastUpdated);
                                }
                            }
                    }

                    // apply source state to cache
                    _cache.AddState(state.Status,
                        state.Inner
                              .GroupBy(kv => kv.Key, kv => kv.Value)
                              .Select(g => KeyValuePair.Create(g.Key, new Group(this, g, state.LastUpdated) as ILiveCollection<TElement>)),
                        delta,
                        state.LastUpdated,
                        false);
                }
            }
            ((ILock)stateLock).DowngradeToReader();

            // translate state
            return _cache.Copy(stateLock);
        }

        protected override void OnCompleted()
        {
            _subscription.Dispose();
            _selfSubscription.Dispose();
        }

        class Group : LiveCollectionView<TElement, ICollectionDelta<TElement>, ICollection<TElement>>, ILiveCollection<TElement>
        {
            private readonly LiveCollectionGroup<TKey, TElement, TIDelta> _parent;
            private readonly CollectionStateCache<TElement, ICollection<TElement>, ICollectionDelta<TElement>> _state = new CollectionStateCache<TElement, ICollection<TElement>, ICollectionDelta<TElement>>(new Collection<TElement>());

            public Group(LiveCollectionGroup<TKey, TElement, TIDelta> parent, IEnumerable<TElement> items, long lastUpdated)
            {
                _parent = parent;
                _innerStateLocker = new Locker(_state.WriteLock);

                // populate
                PushInnerStart(items, lastUpdated);
            }

            public new Action PreInvalidateClients()
            {
                return base.PreInvalidateClients();
            }

            public void Add(TElement item, long lastUpdated)
            {
                var delta = new CollectionDelta<TElement>();
                delta.Insert(-1, new[] { item });
                PushInnerDelta(delta, lastUpdated);
            }

            public void Remove(TElement item, long lastUpdated)
            {
                var delta = new CollectionDelta<TElement>();
                delta.Delete(-1, new[] { item });
                PushInnerDelta(delta, lastUpdated);
            }

            public int Count
            {
                get { return _state.Cache.Count; }
            }

            void PushInnerDelta(CollectionDelta<TElement> delta, long lastUpdated)
            {
                using (_state.WriteLock())
                    _state.AddDelta(delta, lastUpdated, true);
            }

            void PushInnerStart(IEnumerable<TElement> inner, long lastUpdated)
            {
                using (_state.WriteLock())
                    _state.Connect(inner, lastUpdated);
            }

            void PushInnerDisconnect()
            {
                using (_state.WriteLock())
                    _state.Disconnect();
            }

            void PushInnerComplete()
            {
                using (_state.WriteLock())
                    _state.Completed();
            }

            protected override void OnCompleted()
            {
            }

            protected override ICollectionState<TElement, ICollectionDelta<TElement>> InnerGetState(bool notified, IDisposable stateLock)
            {
                if (notified)
                {
                    // update the parent, this will push updates to us
                    _parent.UpdateState();
                }
                ((ILock)stateLock).DowngradeToReader();

                // return copy of our state
                return _state.Copy(stateLock);
            }
        }
    }

    public static partial class Extensions
    {
        public static ILiveDictionary<TKey, ILiveCollection<TElement>> Group<TKey, TElement, TIDelta>(this ILiveCollection<KeyValuePair<TKey, TElement>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, TElement>>
        {
            return new LiveCollectionGroup<TKey, TElement, TIDelta>(source);
        }

        public static ILiveDictionary<TKey, ILiveCollection<TElement>> GroupBy<TKey, TElement, TIDelta>(this ILiveCollection<TElement, TIDelta> source, Func<TElement, ILiveValue<TKey>> keySelector)
            where TIDelta : class, ICollectionDelta<TElement>
        {
            return source
                .SelectStatic(e => KeyValuePair.Create(keySelector(e), e))
                .Unwrap()
                .Group();
        }

        public static ILiveDictionary<TKey, ILiveCollection<TElement>> GroupByStatic<TKey, TElement, TIDelta>(this ILiveCollection<TElement, TIDelta> source, Func<TElement, TKey> keySelector)
            where TIDelta : class, ICollectionDelta<TElement>
        {
            return source
                .SelectStatic(e => KeyValuePair.Create(keySelector(e), e))
                .Group();
        }

        public static ILiveDictionary<TKey, ILiveCollection<TElement>> GroupBy<TSource, TKey, TElement, TIDelta>(this ILiveCollection<TSource, TIDelta> source, Func<TSource, ILiveValue<TKey>> keySelector, Func<TSource, ILiveValue<TElement>> elementSelector)
            where TIDelta : class, ICollectionDelta<TSource>
        {
            return source
                .SelectStatic(s => KeyValuePair.Create(keySelector(s), elementSelector(s)))
                .Unwrap()
                .Group();
        }

        public static ILiveDictionary<TKey, ILiveCollection<TElement>> GroupByStatic<TSource, TKey, TElement, TIDelta>(this ILiveCollection<TSource, TIDelta> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            where TIDelta : class, ICollectionDelta<TSource>
        {
            return source
                .SelectStatic(s => KeyValuePair.Create(keySelector(s), elementSelector(s)))
                .Group();
        }
    }
}
