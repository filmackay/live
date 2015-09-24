using System.Collections;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class CollectionDecorator<T, TICollection> : ICollection<T>
        where TICollection : class, ICollection<T>
    {
        protected TICollection _inner;
        public TICollection Inner { set { _inner = value; } }

        protected virtual bool _Add(T item)
        {
            using (this.Lock())
            {
                var oldCount = _inner.Count;
                _inner.Add(item);
                return (oldCount < _inner.Count);
            }
        }

        public void Add(T item)
        {
            _Add(item);
        }

        public virtual void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return _inner.IsReadOnly; }
        }

        public virtual bool Remove(T item)
        {
            return _inner.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }
    }
}
