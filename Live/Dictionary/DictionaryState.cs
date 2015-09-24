using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    // NOTE: experiemental, not used yet (?)

    public interface IDictionaryState<TKey, TElement> : ICollectionState<KeyValuePair<TKey,TElement>, IDictionaryDelta<TKey,TElement>>
    {
        new IDictionary<TKey, TElement> Inner { get; }
    }

    public class DictionaryState<TKey, TElement> : CollectionState<KeyValuePair<TKey, TElement>, IDictionaryDelta<TKey, TElement>, IDictionary<TKey, TElement>>
    {
        private IDictionary<TKey, TElement> _inner;
        public new IDictionary<TKey, TElement> Inner
        {
            get { return _inner; }
            set { base.Inner = _inner = value; }
        }
    }
}
