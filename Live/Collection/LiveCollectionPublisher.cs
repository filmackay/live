using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public abstract class LiveCollectionPublisher<T, TICollection, TIDelta, TDelta> : LiveCollectionView<T, TIDelta, TICollection>
        where TICollection : class, ICollection<T>
        where TIDelta : class, ICollectionDelta<T>
        where TDelta : CollectionDelta<T, TIDelta, TICollection>, TIDelta, new()
    {
        internal readonly CollectionStateCache<T, TICollection, TIDelta> Committed;
        internal readonly CollectionStateCache<T, TICollection, TIDelta> Uncommitted;

        protected LiveCollectionPublisher(TICollection uncommittedCache, TICollection committedCache, IEnumerable<T> inner)
        {
            Uncommitted = new CollectionStateCache<T, TICollection, TIDelta>(uncommittedCache, inner);
            Committed = new CollectionStateCache<T, TICollection, TIDelta>(committedCache, inner);
            _innerStateLocker = Committed.ReadLock; // only need a read lock as we do no writing in InnerGetState

            if (inner != null)
            {
                // commit initial state
                InnerChanged();
            }

            // setup notifications
            Uncommitted.Notify = () => Publish.OnPublish(Commit);
            Committed.Notify = InnerChanged;
        }

        protected virtual void Commit()
        {
            // apply changes to committed state under write lock
            //using (PauseInnerChanged())
            using (Committed.WriteLock())
                Uncommitted.AddTo(Committed);
        }

        protected internal void PushInnerDelta(TDelta delta, long lastUpdated)
        {
            using (Publish.Transaction())
            using (Uncommitted.WriteLock())
                Uncommitted.AddDelta(delta, lastUpdated, true);
        }

        protected internal void PushInnerConnect(IEnumerable<T> inner, long lastUpdated)
        {
            using (Publish.Transaction())
            using (Uncommitted.WriteLock())
                Uncommitted.Connect(inner, lastUpdated);
        }

        protected internal void PushInnerDisconnect()
        {
            using (Publish.Transaction())
            using (Uncommitted.WriteLock())
                Uncommitted.Disconnect();
        }

        protected override ICollectionState<T, TIDelta> InnerGetState(bool notified, IDisposable stateLock)
        {
            return Committed.Copy(stateLock);
        }

        protected override void OnCompleted()
        {
            using (Publish.Transaction())
            using (Uncommitted.WriteLock())
                Uncommitted.Completed();
        }
    }
}
