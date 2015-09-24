using System.Collections.Generic;


namespace Vertigo.Live
{
    public class LiveCollectionInner<T, TICollection, TIDelta, TDelta, TLiveCollectionPush> : CollectionDecorator<T, TICollection>
        where TICollection : class, ICollection<T>
        where TIDelta : class, ICollectionDelta<T>
        where TDelta : CollectionDelta<T, TIDelta, TICollection>, TIDelta, new()
        where TLiveCollectionPush : LiveCollectionPublisher<T, TICollection, TIDelta, TDelta>
    {
        protected TLiveCollectionPush _parent;
        public TLiveCollectionPush Parent
        {
            set
            {
                _parent = value;
                Inner = _parent.Uncommitted.Cache;
            }
        }

        protected override bool _Add(T item)
        {
            return _Add(item, HiResTimer.Now());
        }

        protected bool _Add(T item, long lastUpdated)
        {
            var delta = new TDelta();
            delta.Insert(-1, new[] { item });
            _parent.PushInnerDelta(delta, lastUpdated);
            return true;
        }

        public bool Add(T item, long lastUpdated)
        {
            return _Add(item, lastUpdated);
        }

        public override bool Remove(T item)
        {
            return Remove(item, HiResTimer.Now());
        }

        public virtual bool Remove(T item, long lastUpdated)
        {
            var delta = new TDelta();
            delta.Delete(-1, new[] { item });
            _parent.PushInnerDelta(delta, lastUpdated);
            return true;
        }

        public void Clear(long lastUpdated)
        {
            _parent.PushInnerConnect(null, lastUpdated);
        }

        public override void Clear()
        {
            Clear(HiResTimer.Now());
        }

        public void Connect(IEnumerable<T> inner)
        {
            Connect(inner, HiResTimer.Now());
        }

        public void Connect(IEnumerable<T> inner, long lastUpdated)
        {
            _parent.PushInnerConnect(inner, lastUpdated);
        }

        public void Disconnect()
        {
            _parent.PushInnerDisconnect();
        }
    }

    public class LiveCollectionInner<T> : LiveCollectionInner<T, ICollection<T>, ICollectionDelta<T>, CollectionDelta<T>, LiveCollection<T>>
    {
    }
}