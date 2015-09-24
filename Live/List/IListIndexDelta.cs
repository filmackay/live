using System.Collections;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public interface IListIndexDelta
    {
        IEnumerable DeleteItems { get; }
        IEnumerable InsertItems { get; }
    }

    public interface IListIndexDelta<out TInner> : IListIndexDelta
    {
        new IEnumerable<TInner> DeleteItems { get; }
        new IEnumerable<TInner> InsertItems { get; }
    }
}