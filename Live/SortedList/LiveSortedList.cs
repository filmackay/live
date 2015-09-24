using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public interface ILiveSortedList<TKey, TValue> : ILiveList<KeyValuePair<TKey, TValue>>
    {
    }

    public class LiveSortedList<TKey, TValue> : LiveCollection<KeyValuePair<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IListDelta<KeyValuePair<TKey, TValue>>, ListDelta<KeyValuePair<TKey, TValue>>, LiveListInner<T>, LiveSortedList<TKey, TValue>>, ILiveSortedList<KeyValuePair<TKey, TValue>>
    {
        public LiveSortedList()
            : this(new List<T>(), null)
        {
        }

        public LiveSortedList(IList<T> publishCache, IEnumerable<T> inner)
            : base(publishCache, new List<T>(), inner)
        {
        }
    }
}