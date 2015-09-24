using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public abstract class LiveSetView<T> : LiveCollectionView<T, ISetDelta<T>, ISet<T>>, ILiveSet<T>
    {
    }
}
