using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public abstract class LiveCollectionView<T, TIDelta, TICollection> : ILiveCollection<T, TIDelta>
        where TIDelta : ICollectionDelta<T>
        where TICollection : ICollection<T>
    {
        private class Subscription : Subscription<LiveCollectionView<T, TIDelta, TICollection>, ICollectionState<T, TIDelta>, CollectionState<T, TIDelta, TICollection>>
        {
            public Subscription(LiveCollectionView<T, TIDelta, TICollection> source, ILiveObserver<ICollectionState<T, TIDelta>> observer)
                : base(source, source._innerStateLocker, observer)
            {
                _lazyNotifyChange = new LazyNotify { OnNotify = NotifyChange };
            }

            private readonly LazyNotify _lazyNotifyChange;
            private Generation Generation;
            private TIDelta PendingDelta
            {
                get
                {
                    var deltaGeneration = Generation as DeltaGeneration;
                    return deltaGeneration != null ? deltaGeneration.FullDelta : default(TIDelta);
                }
            }

            protected override ICollectionState<T, TIDelta> getState(IDisposable stateLock)
            {
                // assumes we already have a _source.Lock()

                // return completed state
                if (_state.Status == StateStatus.Completed)
                {
                    stateLock.SafeDispose();
                    return _state;
                }

                ICollectionState<T, TIDelta> state = null;
                Source._innerChanged.Process(notified =>
                {
                    // avoid recursive applications of inner change
                    if (Source._applyingInnerState == null)
                    {
                        // get inner state
                        Source._applyingInnerState = Source.InnerGetState(notified, stateLock);

                        // apply change to subscribers
                        Subscription[] clients = null;
                        switch (Source._applyingInnerState.Status)
                        {
                            case StateStatus.Reconnecting:
                            case StateStatus.Connecting:
                                // consolidate all observers
                                clients = Source
                                    .Clients
                                    .Where(c => c.Generation != Source._connectingGeneration && !ReferenceEquals(c, this))
                                    .ToArray();
                                clients
                                    .ForEach(s => s.SetGeneration(Source._connectingGeneration, false));
                                break;

                            case StateStatus.Connected:
                                if (Source._applyingInnerState.Delta == null)
                                    break;

                                // move up-to-date subscribers to new generation
                                clients = Source._upToDateGeneration
                                    .Clients
                                    .Where(c => !ReferenceEquals(c, this))
                                    .ToArray();
                                DeltaGeneration newGeneration;
                                if (clients.Length > 0)
                                {
                                    // create new generation
                                    newGeneration = Source._deltaGenerations.New();
                                    clients.ForEach(s => s.SetGeneration(newGeneration, false));
                                }
                                else
                                {
                                    // update existing generation, if it exists
                                    newGeneration = Source._deltaGenerations.Latest();
                                }

                                // update latest generation
                                if (newGeneration != null)
                                    newGeneration.AddDelta(Source._applyingInnerState.Delta);
                                break;

                            case StateStatus.Completing:
                                // tell all clients they are completing because our source is
                                clients = Source.Clients
                                    .Where(c => c._state.Status != StateStatus.Completing && !ReferenceEquals(c, this))
                                    .ToArray();
                                clients
                                    .ForEach(c => c.Dispose());
                                break;
                        }

                        // notify (causes recursion of this method on Consumer.Immediate subscribers)
                        if (clients != null)
                            clients.ForEach(s => s.DoIfNotify());

                        // create state
                        state = CalcState(true);

                        // end potential recursion
                        Source._applyingInnerState = null;
                    }
                    else
                    {
                        // this is a recursive call - copy parent state
                        state = CalcState(false);
                    }
                });

                return state;
            }

            private ICollectionState<T, TIDelta> CalcState(bool detachState)
            {
                // create state
                var state =
                    Source
                        ._applyingInnerState
                        .Extract<T, TIDelta, T, TIDelta, TICollection>(detachState, (inner, delta) => PendingDelta, inner => inner);

                // update status
                state.Status = _state.Status.Add(state.Status.And(Generation == null ? StateStatus.Completed : Generation.Status));
                SetGeneration(Generation == null ? null : Source._upToDateGeneration, false);

                return state;
            }

            protected override void getNotify()
            {
                throw new NotImplementedException();
                //if (_state.Status == StateStatus.Completed)
                //    return;

                //using (Source.Lock())
                //{
                //    // notify client on next change
                //    if (!Source._notNotifiedClients.Contains(this))
                //        Source._notNotifiedClients.Add(this);
                //}
            }

            protected override void _complete(bool graceful)
            {
                // mark as as completed, but leave us in relevant generation so we pickup delta management etc. up until the point of actual completion (on GetStatus)
                if (_state.Status.IsCompleted())
                    return;
                _state.AddInline(StateStatus.Completing);

                // notify if required
                if (graceful)
                    _lazyNotifyChange.Notify(true);
            }

            protected override bool _connect()
            {
                return SetGeneration(Source._connectingGeneration);
            }

            private bool SetGeneration(Generation newGeneration)
            {
                if (ReferenceEquals(Generation, newGeneration))
                    return false;

                var oldGeneration = Generation;

                // remove from old generation
                if (oldGeneration != null)
                    oldGeneration.Remove(this);

                // remember/add to new generation
                Generation = newGeneration;
                if (newGeneration != null)
                    newGeneration.Add(this);

                // indicate if subscription has become pending
                return ((oldGeneration == null || !oldGeneration.IsPending()) &&
                        (newGeneration != null && newGeneration.IsPending()));
            }

            public void SetGeneration(Generation newGeneration, bool notifyNow)
            {
                // notify of change if observer has become pending
                if (SetGeneration(newGeneration))
                    _lazyNotifyChange.Notify(notifyNow);
            }

            public void DoIfNotify()
            {
                _lazyNotifyChange.DoIfNotify();
            }
        }

        private class Generation
        {
            public Generation(StateStatus status)
            {
                Status = status;
            }

            public virtual bool IsPending()
            {
                return Status.IsPending();
            }

            protected readonly HashSet<Subscription> _clients = new HashSet<Subscription>();

            internal void Add(Subscription observer)
            {
                using (this.Lock())
                    _clients.Add(observer);
            }

            internal virtual void Remove(Subscription observer)
            {
                using (this.Lock())
                    _clients.Remove(observer);
            }

            public IEnumerable<Subscription> Clients { get { return _clients; } }
            public StateStatus Status { get; private set; }
        }

        private class DeltaGeneration : Generation
        {
            private readonly DeltaGenerations _parent;

            public DeltaGeneration(DeltaGenerations parent)
                : base(StateStatus.Connected)
            {
                _parent = parent;
            }

            public DeltaGeneration Next;                // next more recent generation
            public CollectionDelta<T, TIDelta, TICollection> Delta;                        // takes observers from here to the next more recent generation

            public override bool IsPending()
            {
                return true;
            }

            private List<TIDelta> _deltas
            {
                get
                {
                    using (this.Lock())
                    {
                        var ret = new List<TIDelta>();
                        var generation = this;
                        while (generation != null)
                        {
                            if (generation.Delta != null)
                                ret.Add(generation.Delta);
                            generation = generation.Next;
                        }
                        return ret;
                    }
                }
            }

            internal override void Remove(Subscription client)
            {
                using (this.Lock())
                {
                    base.Remove(client);

                    // remove generation if no longer used
                    if (_clients.Count == 0)
                        _parent.Remove(this);
                }
            }

            public void AddDelta(TIDelta delta)
            {
                if (!Delta.HasChange())
                    Delta = delta.ToMutable<T, TIDelta, TICollection>();
                else
                    Delta.Add(delta);
            }

            public TIDelta FullDelta
            {
                get { return _deltas.Merge<T, TIDelta, TICollection>(); }
            }
        }

        private class DeltaGenerations : IEnumerable<DeltaGeneration>
        {
            private readonly List<DeltaGeneration> _list = new List<DeltaGeneration>();

            public IEnumerable<Subscription> Clients
            {
                get { return _list.SelectMany(g => g.Clients); }
            }

            public DeltaGeneration Latest()
            {
                using (this.Lock())
                {
                    Debug.Assert(_list.Count == 0 || _list[0].Clients.Any());
                    return _list.Count == 0 ? null : _list[0];
                }
            }

            public DeltaGeneration New()
            {
                using (this.Lock())
                {
                    // any generations?
                    if (_list.Count > 0)
                    {
                        // use first generation if no changes yet?
                        if (!_list[0].Delta.HasChange())
                            return _list[0];

                        // first generation should still be in use, otherwise it should be empty
                        Debug.Assert(_list[0].Clients.Any());
                    }

                    // create a new generation
                    return CreateGeneration();
                }
            }

            private DeltaGeneration CreateGeneration()
            {
                Debug.Assert(_list.Count == 0 || _list[0].Next == null);

                using (this.Lock())
                {
                    // create generation
                    var generation = new DeltaGeneration(this);

                    // add to list and link
                    if (_list.Count > 0)
                        _list[0].Next = generation;
                    _list.Insert(0, generation);

                    return generation;
                }
            }

            public void Remove(DeltaGeneration generation)
            {
                using (this.Lock())
                {
                    Debug.Assert(!generation.Clients.Any());

                    var index = _list.IndexOf(generation);
                    Debug.Assert(index != -1);

                    if (index < (_list.Count - 1))
                    {
                        // there is an older generation - merge
                        var olderGeneration = _list[index + 1];
                        olderGeneration.AddDelta(generation.Delta);
                        olderGeneration.Next = generation.Next;
                    }
                    _list.RemoveAt(index);

                    Debug.Assert(_list.Count == 0 || _list[0].Clients.Any());
                    Debug.Assert(_list.Count == 0 || _list[0].Next == null);
                }
            }

            #region IEnumerable<DeltaGeneration>

            public IEnumerator<DeltaGeneration> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        protected LiveCollectionView()
        {
            _binding = new Lazy<LiveListBinding>(() => new LiveListBinding(this.ToLiveList().ToUntyped()), true);
            _innerChanged = new NotifyLock
            {
                OnNotify = () => PreInvalidateClients()(),
            };
        }

        private readonly NotifyLock _innerChanged;
        private readonly DeltaGenerations _deltaGenerations = new DeltaGenerations();
        private readonly Generation _upToDateGeneration = new Generation(StateStatus.Connected);
        private readonly Generation _connectingGeneration = new Generation(StateStatus.Connecting);

        private IEnumerable<Subscription> Clients
        {
            get
            {
                return _upToDateGeneration.Clients.Concat(
                    _connectingGeneration.Clients.Concat(
                    _deltaGenerations.Clients));
            }
        }

        private readonly Lazy<LiveListBinding> _binding;
        public LiveListBinding Binding
        {
            get { return _binding.Value; }
        }

        protected virtual Action PreInvalidateClients()
        {
            // get all clients
            Subscription[] clients;
            using (this.Lock())
                clients =
                    _upToDateGeneration
                        .Clients
                        .ToArray();
            if (clients.Length == 0)
                return () => { };

            // move to new generation first
            var newGeneration = _deltaGenerations.New();
            clients.ForEach(s => s.SetGeneration(newGeneration, false));

            // then notify
            return () => clients.ForEach(s => s.DoIfNotify());
        }

        //private CollectionState<T, TIDelta, TDelta> _applyingInnerState;
        private ICollectionState<T, TIDelta> _applyingInnerState;

        public IDisposable Subscribe(ILiveObserver<ICollectionState<T, TIDelta>> observer)
        {
            var subscription = new Subscription(this, observer);
            subscription.Start();
            return subscription;
        }

        protected Locker _innerStateLocker;

        protected abstract ICollectionState<T, TIDelta> InnerGetState(bool notified, IDisposable stateLock);

        protected void InnerChanged()
        {
            _innerChanged.Notify();
        }

        public virtual Type InnerType
        { get { return typeof(T); } }

        protected abstract void OnCompleted();
    }
}