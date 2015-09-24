using System.Collections.Generic;

namespace Vertigo.Live
{
    class KeyValuePairComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
    {
        private readonly IComparer<TKey> _keyComparer;
        public static KeyValuePairComparer<TKey, TValue> Default = new KeyValuePairComparer<TKey, TValue>();

        public KeyValuePairComparer(IComparer<TKey> keyComparer = null)
        {
            _keyComparer = keyComparer ?? Comparer<TKey>.Default;
        }

        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return _keyComparer.Compare(x.Key, y.Key);
        }
    }
}