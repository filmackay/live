using System;
using System.Collections;
using System.Collections.Generic;


namespace Vertigo.Live
{
    public class LiveDictionaryInner<TKey, TValue> : LiveCollectionInner<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>, IDictionaryDelta<TKey, TValue>, DictionaryDelta<TKey, TValue>, LiveDictionary<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            Add(key, value, HiResTimer.Now());
        }

        public void Add(TKey key, TValue value, long lastUpdated)
        {
            var delta = new DictionaryDelta<TKey, TValue>();
            delta.Insert(-1, new[] { new KeyValuePair<TKey, TValue>(key,value) });
            _parent.PushInnerDelta(delta, lastUpdated);
        }

        public bool Remove(TKey key)
        {
            return Remove(key, HiResTimer.Now());
        }

        public bool Remove(TKey key, long lastUpdated)
        {
            TValue oldValue;
            if (!_inner.TryGetValue(key, out oldValue))
                return false;
            
            var delta = new DictionaryDelta<TKey, TValue>();
            delta.Delete(-1, new[] { new KeyValuePair<TKey, TValue>(key, oldValue) });
            _parent.PushInnerDelta(delta, lastUpdated);

            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _inner[key]; }
            set { Add(key, value); }
        }

        public ICollection<TKey> Keys
        {
            get { return _inner.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _inner.Values; }
        }
    }
}