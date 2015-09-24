using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public class MultiMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly NullDictionary<TKey, List<TValue>> _dictionary = new NullDictionary<TKey, List<TValue>>();

        public void Add(KeyValuePair<TKey, TValue> keyValue)
        {
            Add(keyValue.Key, keyValue.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> list;
            if (_dictionary.TryGetValue(key, out list))
            {
                list.Add(value);
            }
            else
            {
                list = new List<TValue> { value };
                _dictionary.Add(key, list);
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            values.ForEach(Add);
        }

        public ICollection<TKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _dictionary.SelectMany(kv => kv.Value).ToArray(); }
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            List<TValue> list;
            if (!_dictionary.TryGetValue(item.Key, out list))
                return false;

            return list.Contains(item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            EnumerableKeyValuePair.ToArray().CopyTo(array, arrayIndex);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            List<TValue> list;
            if (!_dictionary.TryGetValue(key, out list))
            {
                value = default(TValue);
                return false;
            }

            value = list[0];
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new ArgumentOutOfRangeException();
                return value;
            }

            set
            {
                Add(key, value);
            }
        }

        public IEnumerable<TValue> Items(TKey key)
        {
            using (this.Lock())
            {
                List<TValue> list;
                if (_dictionary.TryGetValue(key, out list))
                    return list;
                return new TValue[0];
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> keyValue)
        {
            return Remove(keyValue.Key, keyValue.Value);
        }

        public int Count
        {
            get { return _dictionary.Sum(kv => kv.Value.Count()); }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary<TKey,TValue>)_dictionary).IsReadOnly; }
        }

        public bool Remove(TKey key, TValue value)
        {
            List<TValue> list;
            if (!_dictionary.TryGetValue(key, out list))
                return false;
            if (!list.Remove(value))
                return false;
            if (list.Count == 0)
                _dictionary.Remove(key);
            return true;
        }

        public bool Remove(TKey key)
        {
            // removes all items keyed to this value
            return _dictionary.Remove(key);
        }

        public TValue RemoveFirst(TKey key)
        {
            List<TValue> list;
            if (!_dictionary.TryGetValue(key, out list))
                throw new KeyNotFoundException();
            var ret = list[0];
            if (list.Count == 1)
                _dictionary.Remove(key);
            else
                list.RemoveAt(0);
            return ret;
        }

        public void RemoveRange(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            values.ForEach(kv => Remove(kv));
        }

        private IEnumerable<KeyValuePair<TKey,TValue>> EnumerableKeyValuePair
        {
            get
            {
                return _dictionary
                    .SelectMany(kv => kv.Value.Select(v => KeyValuePair.Create(kv.Key, v)));
            }
        } 

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return EnumerableKeyValuePair.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
