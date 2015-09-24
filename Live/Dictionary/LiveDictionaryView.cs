using System.Collections.Generic;

namespace Vertigo.Live
{
    public abstract class LiveDictionaryView<TKey, TValue> : LiveCollectionView<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>, IDictionary<TKey, TValue>>, ILiveDictionary<TKey, TValue>
    {
        public ILiveValue<TValue> this[ILiveValue<TKey> key]
        {
            get { return this.Value(key); }
        }
    }
}
