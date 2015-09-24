using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public class IndexNodeComparer<T> : IEqualityComparer<IIndexNode<IListIndexDelta<T>>>
    {
        public bool Equals(IIndexNode<IListIndexDelta<T>> x, IIndexNode<IListIndexDelta<T>> y)
        {
            var ret = (x.Index == y.Index
                    && x.DenseIndex == y.DenseIndex
                    && x.Data.InsertItems.SequenceEqual(y.Data.InsertItems)
                    && x.Data.DeleteItems.SequenceEqual(y.Data.DeleteItems));
            return ret;
        }

        public int GetHashCode(IIndexNode<IListIndexDelta<T>> obj)
        {
            return obj.GetHashCode();
        }
    }
}