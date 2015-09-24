using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveSetInner<T> : LiveSetInner<T, LiveSetInner<T>, LiveSet<T>>
    {
    }

    public class LiveSetInner<T, TLiveSetInner, TLiveSet> : LiveCollectionInner<T, ISet<T>, ISetDelta<T>, SetDelta<T>, TLiveSet>, ISet<T>
        where TLiveSetInner : LiveSetInner<T, TLiveSetInner, TLiveSet>, new()
        where TLiveSet : LiveSet<T, TLiveSetInner, TLiveSet>
    {
        bool ISet<T>.Add(T item)
        {
            return base._Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _inner.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _inner.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _inner.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _inner.IsProperSubsetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _inner.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _inner.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _inner.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _inner.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _inner.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _inner.UnionWith(other);
        }
    }
}