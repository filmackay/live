using System.Collections;
using System.Collections.Generic;


namespace Vertigo.Live
{
    public class LiveListInner<T> : LiveCollectionInner<T, IList<T>, IListDelta<T>, ListDelta<T>, LiveList<T>>, IList<T>
    {
        public int IndexOf(T item)
        {
            return _inner.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Insert(index, item, HiResTimer.Now());
        }

        public void Insert(int index, T item, long lastUpdated)
        {
            var delta = new ListDelta<T>();
            delta.Insert(index, new[] { item });
            _parent.PushInnerDelta(delta, lastUpdated);
        }

        public void RemoveAt(int index)
        {
            RemoveAt(index, HiResTimer.Now());
        }

        public void RemoveAt(int index, long lastUpdated)
        {
            var delta = new ListDelta<T>();
            delta.Delete(index, new[] { _inner[index] });
            _parent.PushInnerDelta(delta, lastUpdated);
        }

        protected override bool _Add(T item)
        {
            Insert(_inner.Count, item);
            return true;
        }

        public override bool Remove(T item)
        {
            return Remove(item, HiResTimer.Now());
        }

        public override bool Remove(T item, long lastUpdated)
        {
            var index = _inner.IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index, lastUpdated);
            return true;
        }

        public T this[int index]
        {
            get { return _inner[index]; }
            set { Update(index, value, HiResTimer.Now()); }
        }

        public void Update(int index, T value, long lastUpdated)
        {
            var delta = new ListDelta<T>();
            delta.Update(index, _inner[index], value);
            _parent.PushInnerDelta(delta, lastUpdated);
        }
    }
}