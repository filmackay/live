using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public class CollectionStateCache<T, TICollection, TIDelta>
        where TIDelta : class, ICollectionDelta<T>
        where TICollection : ICollection<T>
    {
        private readonly ReaderWriterSpinLock _lock = new ReaderWriterSpinLock();    // lock with no thread affinity
        private readonly NotifyLock _notify = new NotifyLock();

        public CollectionStateCache(TICollection cache, IEnumerable<T> inner = null)
        {
            _lock.ID = GetHashCode();
            Cache = cache;
            if (inner != null)
            {
                // start with initial inner
                if (!ReferenceEquals(inner, Cache))
                {
                    if (Cache.Count > 0)
                        Cache.Clear();
                    Cache.AddRange(inner);
                }
                Status = StateStatus.Connected;
            }
        }

        public TICollection Cache { get; private set; }
        public CollectionDelta<T, TIDelta, TICollection> Delta { get; private set; }
        public StateStatus Status { get; private set; }
        public long LastUpdated { get; private set; }

        public Action Notify
        {
            get { return _notify.OnNotify; }
            set { _notify.OnNotify = value; }
        }

        public void Connect(IEnumerable<T> inner, long now)
        {
            VerifyWriteLock();

            Status = Status.Add(StateStatus.Connecting);
            LastUpdated = Math.Max(LastUpdated, now);
            Delta = null;

            // if starting on itself, use existing values);
            if (inner == null || !ReferenceEquals(inner, Cache))
            {
                Cache.Clear();
                if (inner != null)
                    Cache.AddRange(inner);
            }

            _notify.Notify();
        }

        public void Disconnect()
        {
            VerifyWriteLock();
            Status = Status.Add(StateStatus.Disconnecting);
            _notify.Notify();
        }

        public void Completed()
        {
            VerifyWriteLock();
            Status = Status.Add(StateStatus.Completing);
            _notify.Notify();
        }

        private void _addDelta(TIDelta newDelta, bool applyToCache)
        {
            // apply delta to our cache
            CollectionDelta<T, TIDelta, TICollection> newDeltaClass = null;
            if (applyToCache)
            {
                newDeltaClass = newDelta.ToMutable<T, TIDelta, TICollection>();
                newDeltaClass.ApplyTo(Cache);
            }

            // keep delta
            if (Delta == null)
            {
                if (newDeltaClass == null)
                    newDeltaClass = newDelta.ToMutable<T, TIDelta, TICollection>();
                Delta = newDeltaClass;
            }
            else
                Delta.Add(newDelta);
        }

        public void AddDelta(TIDelta newDelta, long lastUpdated, bool applyToCache)
        {
            LastUpdated = Math.Max(LastUpdated, lastUpdated);

            // make sure we have write lock and a relevant delta
            VerifyWriteLock();
            if (!newDelta.HasChange())
                return;

            // add and notify of change
            _addDelta(newDelta, applyToCache);
            _notify.Notify();
        }

        public void AddState(StateStatus status, IEnumerable<T> inner, TIDelta delta, long lastUpdated, bool applyToCache)
        {
            VerifyWriteLock();

            if (status.IsConnecting())
                Connect(inner, lastUpdated);
            else
            {
                var notify = false;
                if (Status != status)
                {
                    var newStatus = Status.Add(status);
                    if (newStatus != Status)
                    {
                        Status = newStatus;
                        notify = true;
                    }
                }

                LastUpdated = Math.Max(LastUpdated, lastUpdated);

                if (delta.HasChange())
                {
                    _addDelta(delta, applyToCache);
                    notify = true;
                }

                if (notify)
                    _notify.Notify();
            }
        }

        private void Advance()
        {
            Status = Status.Next();
            Delta = null;
        }

        private void VerifyWriteLock()
        {
            if (!_lock.IsWriteLockHeld)
                throw new InvalidOperationException("Write lock required");
        }

        public void AddTo(CollectionStateCache<T, TICollection, TIDelta> target)
        {
            _notify.Process(notified =>
                {
                    target.AddState(Status, Cache, Delta, LastUpdated, true);
                    Advance();
                });
        }

        public CollectionState<T, TIDelta, TICollection> Copy(IDisposable stateLock)
        {
            return Extract<T, TIDelta, TICollection>(delta => delta, inner => inner, stateLock);
        }

        public CollectionState<TResult, TResultIDelta, TResultICollection> Extract<TResult, TResultIDelta, TResultICollection>(Func<TIDelta, TResultIDelta> deltaConverter, Func<TICollection, IEnumerable<TResult>> innerSelector, IDisposable stateLock)
            where TResultIDelta : ICollectionDelta<TResult>
            where TResultICollection : ICollection<TResult>
        {
            // return state
            var state = new CollectionState<TResult, TResultIDelta, TResultICollection>();
            _notify.Process(notified =>
            {
                state.SetState(Status, deltaConverter(Delta), innerSelector(Cache), LastUpdated, stateLock);
                Advance();
            });

            return state;
        }

        public Locker ReadLock { get { return _readLock; } }
        private IDisposable _readLock(bool block = true)
        {
            return _lock.ReadLock(block);
        }

        public ReaderWriterLocker WriteLock { get { return _writeLock; } }
        private ILock _writeLock(bool block = true)
        {
            return _lock.WriteLock(block);
        }
    }

    public static partial class Extensions
    {
        public static CollectionState<TResult, ICollectionDelta<TResult>, ICollection<TResult>> Extract<T, TICollection, TIDelta, TResult>(this CollectionStateCache<T, TICollection, TIDelta> stateCache, Func<TIDelta, ICollectionDelta<TResult>> deltaConverter, Func<TICollection, IEnumerable<TResult>> innerSelector, IDisposable stateLock)
            where TIDelta : class, ICollectionDelta<T>
            where TICollection : ICollection<T>
        {
            return stateCache.Extract<TResult, ICollectionDelta<TResult>, ICollection<TResult>>(deltaConverter, innerSelector, stateLock);
        }

        public static CollectionState<TResult, IListDelta<TResult>, IList<TResult>> Extract<T, TICollection, TIDelta, TResult>(this CollectionStateCache<T, TICollection, TIDelta> stateCache, Func<TIDelta, IListDelta<TResult>> deltaConverter, Func<TICollection, IEnumerable<TResult>> innerSelector, IDisposable stateLock)
            where TIDelta : class, ICollectionDelta<T>
            where TICollection : ICollection<T>
        {
            return stateCache.Extract<TResult, IListDelta<TResult>, IList<TResult>>(deltaConverter, innerSelector, stateLock);
        }
    }
}
