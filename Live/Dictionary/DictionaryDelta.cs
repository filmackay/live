using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public interface IDictionaryDelta<TKey, TValue> : ICollectionDelta<KeyValuePair<TKey, TValue>>
    {
    }

    public sealed class DictionaryDelta<TKey, TValue> : CollectionDelta<KeyValuePair<TKey,TValue>, IDictionaryDelta<TKey,TValue>, IDictionary<TKey,TValue>>, IDictionaryDelta<TKey, TValue>
    {
        private readonly Lazy<NullDictionary<TKey, TValue>> _inserts = new Lazy<NullDictionary<TKey, TValue>>();
        private readonly Lazy<NullDictionary<TKey, TValue>> _deletes = new Lazy<NullDictionary<TKey, TValue>>();

        public override IEnumerable<KeyValuePair<TKey, TValue>> Inserts
        {
            get
            {
                return
                    _inserts.IsValueCreated && _inserts.Value.Count > 0
                        ? _inserts.Value
                        : null;
            }
        }

        public override IEnumerable<KeyValuePair<TKey, TValue>> Deletes
        {
            get
            {
                return
                    _deletes.IsValueCreated && _deletes.Value.Count > 0
                        ? _deletes.Value
                        : null;
            }
        }

        public void Clear()
        {
            if (_inserts.IsValueCreated)
                _inserts.Value.Clear();
            if (_deletes.IsValueCreated)
                _deletes.Value.Clear();
        }

        public override void Insert(int index, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (_deletes.Value.Contains(item))
                        _deletes.Value.Remove(item.Key);
                    else
                        _inserts.Value.Add(item.Key, item.Value);
                }
            }
        }

        public override void Update(int index, KeyValuePair<TKey, TValue> oldItem, KeyValuePair<TKey, TValue> newItem)
        {
            using (this.Lock())
            {
                Delete(index, new[] { oldItem });
                Insert(index, new[] { newItem });
            }
        }

        public override void Delete(int index, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (_inserts.Value.Contains(item))
                        _inserts.Value.Remove(item.Key);
                    else
                        _deletes.Value.Add(item.Key, item.Value);
                }
            }
        }
    }
}
