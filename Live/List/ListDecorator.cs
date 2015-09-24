using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class ListDecorator<T> : CollectionDecorator<T, IList<T>>, IList<T>
    {
        public int IndexOf(T item)
        {
            return _inner.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return _inner[index]; }
            set { _inner[index] = value; }
        }
    }
}
