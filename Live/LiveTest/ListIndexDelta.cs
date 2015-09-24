using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class ListIndexDelta<T> : IListIndexDelta<T>
    {
        public IEnumerable<T> DeleteItems = Enumerable.Empty<T>();
        IEnumerable<T> IListIndexDelta<T>.DeleteItems { get { return DeleteItems; } }
        IEnumerable IListIndexDelta.DeleteItems { get { return DeleteItems; } }

        public IEnumerable<T> InsertItems = Enumerable.Empty<T>();
        IEnumerable<T> IListIndexDelta<T>.InsertItems { get { return InsertItems; } }
        IEnumerable IListIndexDelta.InsertItems { get { return InsertItems; } }
    }
}