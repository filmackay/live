using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live.Test
{
    public class IndexNode<T> : IIndexNode<IListIndexDelta<T>>
    {
        public int Index { get; set; }
        public int DenseIndex { get; set; }
        public IListIndexDelta<T> Data { get; set; }
        public IIndexNode<IListIndexDelta<T>> Next { get; set; }
        public IIndexNode<IListIndexDelta<T>> Previous { get; set; }
    }
}