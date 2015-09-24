using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public class LiveDictionaryGroup<TGroupKey, TUniqueElement> : LiveDictionaryView<TGroupKey, ILiveSet<TUniqueElement>>
    {
        public LiveDictionaryGroup(ILiveDictionary<TUniqueElement, TGroupKey> source)
        {
            _innerStateLocker = new Locker(_cache.WriteLock);

            _subscription = source.Subscribe(_observer = source.CreateObserver(InnerChanged));
            _subscription = source.Subscribe(_observer = source.CreateObserver(InnerChanged));

            // subscribe to self, on behalf of groups
            _selfSubscription = Subscribe(_selfObserver = this.CreateObserver(InnerChanged));
        }

        private readonly CollectionStateCache<KeyValuePair<TGroupKey, ILiveSet<TUniqueElement>>, IDictionary<TGroupKey, ILiveSet<TUniqueElement>>, IDictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>>> _cache = new CollectionStateCache<KeyValuePair<TGroupKey, ILiveSet<TUniqueElement>>, IDictionary<TGroupKey, ILiveSet<TUniqueElement>>, IDictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>>>(new Dictionary<TGroupKey, ILiveSet<TUniqueElement>>());
        private readonly LiveObserver<ICollectionState<KeyValuePair<TUniqueElement, TGroupKey>, IDictionaryDelta<TUniqueElement, TGroupKey>>> _observer;
        private readonly LiveObserver<ICollectionState<KeyValuePair<TGroupKey, ILiveSet<TUniqueElement>>, IDictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>>>> _selfObserver;
        private readonly IDisposable _subscription;
        private readonly IDisposable _selfSubscription;

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

        protected override ICollectionState<KeyValuePair<TGroupKey, ILiveSet<TUniqueElement>>, IDictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>>> InnerGetState(bool notified, IDisposable stateLock)
        {
            if (notified)
            {
                // get state
                using (var state = _observer.GetState())
                {
                    // convert delta
                    DictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>> delta = null;
                    if (state.Delta != null)
                    {
                        delta = new DictionaryDelta<TGroupKey, ILiveSet<TUniqueElement>>();

                        // handle deletes
                        foreach (var remove in state.Delta.Deletes)
                        {
                            var liveGrouping = _cache.Cache[remove.Value];
                            var group = liveGrouping as Group;

                            // remove from group
                            group.Remove(remove.Key, state.LastUpdated);

                            // remove group entirely if it is empty
                            if (group.Count == 0)
                            {
                                // group is now empty - remove it
                                delta.Delete(-1, new[] { KeyValuePair.Create(remove.Value, liveGrouping) });
                                _cache.Cache.Remove(remove.Value);
                            }
                        }

                        // handle inserts
                        foreach (var add in state.Delta.Inserts)
                        {
                            ILiveSet<TUniqueElement> liveGrouping;
                            if (!_cache.Cache.TryGetValue(add.Value, out liveGrouping))
                            {
                                // create new group
                                liveGrouping = new Group(this, new[] { add.Key }, state.LastUpdated);
                                _cache.Cache.Add(add.Value, liveGrouping);
                                delta.Insert(-1, new[] { KeyValuePair.Create(add.Value, liveGrouping) });
                            }
                            else
                            {
                                // add to group
                                (liveGrouping as Group).Add(add.Key, state.LastUpdated);
                            }
                        }
                    }

                    // apply source state to cache
                    _cache.AddState(state.Status,
                        state.Inner
                              .GroupBy(kv => kv.Value, kv => kv.Key)
                              .Select(g => KeyValuePair.Create(g.Key, new Group(this, g, state.LastUpdated) as ILiveSet<TUniqueElement>)),
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

        class Group : LiveSetView<TUniqueElement>
        {
            private readonly LiveDictionaryGroup<TGroupKey, TUniqueElement> _parent;
            private readonly CollectionStateCache<TUniqueElement, ISet<TUniqueElement>, ISetDelta<TUniqueElement>> _state = new CollectionStateCache<TUniqueElement, ISet<TUniqueElement>, ISetDelta<TUniqueElement>>(new HashSet<TUniqueElement>());

            public Group(LiveDictionaryGroup<TGroupKey, TUniqueElement> parent, IEnumerable<TUniqueElement> items, long lastUpdated)
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

            public void Add(TUniqueElement item, long lastUpdated)
            {
                var delta = new SetDelta<TUniqueElement>();
                delta.Insert(-1, new[] { item });
                PushInnerDelta(delta, lastUpdated);
            }

            public void Remove(TUniqueElement item, long lastUpdated)
            {
                var delta = new SetDelta<TUniqueElement>();
                delta.Delete(-1, new[] { item });
                PushInnerDelta(delta, lastUpdated);
            }

            public int Count
            {
                get { return _state.Cache.Count; }
            }

            void PushInnerDelta(SetDelta<TUniqueElement> delta, long lastUpdated)
            {
                using (_state.WriteLock())
                    _state.AddDelta(delta, lastUpdated, true);
            }

            void PushInnerStart(IEnumerable<TUniqueElement> inner, long lastUpdated)
            {
                using (_state.WriteLock())
                    _state.Connect(inner, lastUpdated);
            }

            void PushInnerDisconnect()
            {
                using (_state.WriteLock())
                    _state.Disconnect();
            }

            void PushInnerCompleted()
            {
                using (_state.WriteLock())
                    _state.Completed();
            }

            protected override ICollectionState<TUniqueElement, ISetDelta<TUniqueElement>> InnerGetState(bool notified, IDisposable stateLock)
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

            protected override void OnCompleted()
            {
            }
        }
    }
    public static partial class Extensions
    {
        public static ILiveDictionary<TGroupKey, ILiveSet<TUniqueElement>> Group<TGroupKey, TUniqueElement>(this ILiveDictionary<TUniqueElement, TGroupKey> source)
        {
            return new LiveDictionaryGroup<TGroupKey, TUniqueElement>(source);
        }
    }
}
