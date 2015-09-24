using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

namespace Vertigo
{
    [Serializable, DebuggerTypeProxy(typeof(NullDictionary<,>.DebugView)), DebuggerDisplay("Count = {Count}"), ComVisible(false)]
    public class NullDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ISerializable, IDeserializationCallback
    {
        // Fields
        private object _syncRoot;
        private int[] buckets;
        private IEqualityComparer<TKey> comparer;
        private int count;
        private Entry[] entries;
        private int freeCount;
        private int freeList;
        private KeyCollection keys;
        private SerializationInfo m_siInfo;
        private ValueCollection values;
        private int version;

        // Methods
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public NullDictionary() : this(0, null)
        {
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public NullDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null)
        {
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public NullDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer)
        {
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public NullDictionary(int capacity) : this(capacity, null)
        {
        }

        public NullDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : this((dictionary != null) ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                this.Add(pair.Key, pair.Value);
            }
        }

        public NullDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            if (capacity > 0)
            {
                this.Initialize(capacity);
            }
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        protected NullDictionary(SerializationInfo info, StreamingContext context)
        {
            this.m_siInfo = info;
        }

        // support null keys
        private int KeyHashCode(TKey key)
        {
            return key == null ? 0 : comparer.GetHashCode(key);
        }

        private bool KeyEquals(TKey a, TKey b)
        {
            if (a == null && b == null)
                return true;
            if (a == null && b != null || a != null && b == null)
                return false;
            return comparer.Equals(a, b);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void Add(TKey key, TValue value)
        {
            this.Insert(key, value, true);
        }

        public void Clear()
        {
            if (this.count > 0)
            {
                for (int i = 0; i < this.buckets.Length; i++)
                {
                    this.buckets[i] = -1;
                }
                Array.Clear(this.entries, 0, this.count);
                this.freeList = -1;
                this.count = 0;
                this.freeCount = 0;
                this.version++;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return (this.FindEntry(key) >= 0);
        }

        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                for (int i = 0; i < this.count; i++)
                {
                    if ((this.entries[i].hashCode >= 0) && (this.entries[i].value == null))
                    {
                        return true;
                    }
                }
            }
            else
            {
                EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
                for (int j = 0; j < this.count; j++)
                {
                    if ((this.entries[j].hashCode >= 0) && comparer.Equals(this.entries[j].value, value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [SecuritySafeCritical]
        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if ((index < 0) || (index > array.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((array.Length - index) < this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        private int FindEntry(TKey key)
        {
            if (this.buckets != null)
            {
                int num = KeyHashCode(key) & 0x7fffffff;
                for (int i = this.buckets[num % this.buckets.Length]; i >= 0; i = this.entries[i].next)
                {
                    if ((this.entries[i].hashCode == num) && KeyEquals(this.entries[i].key, key))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator((NullDictionary<TKey, TValue>) this, 2);
        }

        [SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("Version", this.version);
            info.AddValue("Comparer", this.comparer, typeof(IEqualityComparer<TKey>));
            info.AddValue("HashSize", (this.buckets == null) ? 0 : this.buckets.Length);
            if (this.buckets != null)
            {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[this.Count];
                this.CopyTo(array, 0);
                info.AddValue("KeyValuePairs", array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        internal TValue GetValueOrDefault(TKey key)
        {
            int index = this.FindEntry(key);
            if (index >= 0)
            {
                return this.entries[index].value;
            }
            return default(TValue);
        }

        private void Initialize(int capacity)
        {
            int prime = HashHelpers.GetPrime(capacity);
            this.buckets = new int[prime];
            for (int i = 0; i < this.buckets.Length; i++)
            {
                this.buckets[i] = -1;
            }
            this.entries = new Entry[prime];
            this.freeList = -1;
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            int freeList;
            //if (key == null)
            //    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            if (this.buckets == null)
            {
                this.Initialize(0);
            }
            int num = KeyHashCode(key) & 0x7fffffff;
            int index = num % this.buckets.Length;
            for (int i = this.buckets[index]; i >= 0; i = this.entries[i].next)
            {
                if ((this.entries[i].hashCode == num) && KeyEquals(this.entries[i].key, key))
                {
                    if (add)
                        throw new DuplicateKeyException(key);
                    this.entries[i].value = value;
                    this.version++;
                    return;
                }
            }
            if (this.freeCount > 0)
            {
                freeList = this.freeList;
                this.freeList = this.entries[freeList].next;
                this.freeCount--;
            }
            else
            {
                if (this.count == this.entries.Length)
                {
                    this.Resize();
                    index = num % this.buckets.Length;
                }
                freeList = this.count;
                this.count++;
            }
            this.entries[freeList].hashCode = num;
            this.entries[freeList].next = this.buckets[index];
            this.entries[freeList].key = key;
            this.entries[freeList].value = value;
            this.buckets[index] = freeList;
            this.version++;
        }

        private static bool IsCompatibleKey(object key)
        {
            return (key is TKey);
        }

        public virtual void OnDeserialization(object sender)
        {
            if (this.m_siInfo != null)
            {
                int num = this.m_siInfo.GetInt32("Version");
                int num2 = this.m_siInfo.GetInt32("HashSize");
                this.comparer = (IEqualityComparer<TKey>) this.m_siInfo.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
                if (num2 != 0)
                {
                    this.buckets = new int[num2];
                    for (int i = 0; i < this.buckets.Length; i++)
                    {
                        this.buckets[i] = -1;
                    }
                    this.entries = new Entry[num2];
                    this.freeList = -1;
                    KeyValuePair<TKey, TValue>[] pairArray = (KeyValuePair<TKey, TValue>[]) this.m_siInfo.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
                    if (pairArray == null)
                    {
                        throw new SerializationException();
                    }
                    for (int j = 0; j < pairArray.Length; j++)
                    {
                        this.Insert(pairArray[j].Key, pairArray[j].Value, true);
                    }
                }
                else
                {
                    this.buckets = null;
                }
                this.version = num;
                this.m_siInfo = null;
            }
        }

        public bool Remove(TKey key)
        {
            if (this.buckets != null)
            {
                int num = KeyHashCode(key) & 0x7fffffff;
                int index = num % this.buckets.Length;
                int num3 = -1;
                for (int i = this.buckets[index]; i >= 0; i = this.entries[i].next)
                {
                    if ((this.entries[i].hashCode == num) && KeyEquals(this.entries[i].key, key))
                    {
                        if (num3 < 0)
                        {
                            this.buckets[index] = this.entries[i].next;
                        }
                        else
                        {
                            this.entries[num3].next = this.entries[i].next;
                        }
                        this.entries[i].hashCode = -1;
                        this.entries[i].next = this.freeList;
                        this.entries[i].key = default(TKey);
                        this.entries[i].value = default(TValue);
                        this.freeList = i;
                        this.freeCount++;
                        this.version++;
                        return true;
                    }
                    num3 = i;
                }
            }
            return false;
        }

        private void Resize()
        {
            int prime = HashHelpers.GetPrime(this.count * 2);
            int[] numArray = new int[prime];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = -1;
            }
            Entry[] destinationArray = new Entry[prime];
            Array.Copy(this.entries, 0, destinationArray, 0, this.count);
            for (int j = 0; j < this.count; j++)
            {
                int index = destinationArray[j].hashCode % prime;
                destinationArray[j].next = numArray[index];
                numArray[index] = j;
            }
            this.buckets = numArray;
            this.entries = destinationArray;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            this.Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int index = this.FindEntry(keyValuePair.Key);
            return ((index >= 0) && EqualityComparer<TValue>.Default.Equals(this.entries[index].value, keyValuePair.Value));
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            this.CopyTo(array, index);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int index = this.FindEntry(keyValuePair.Key);
            if ((index >= 0) && EqualityComparer<TValue>.Default.Equals(this.entries[index].value, keyValuePair.Value))
            {
                this.Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator((NullDictionary<TKey, TValue>) this, 2);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
            {
                throw new ArgumentException("Rank multi dim not supported");
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException();
            }
            if ((index < 0) || (index > array.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((array.Length - index) < this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            KeyValuePair<TKey, TValue>[] pairArray = array as KeyValuePair<TKey, TValue>[];
            if (pairArray != null)
            {
                this.CopyTo(pairArray, index);
            }
            else if (array is DictionaryEntry[])
            {
                DictionaryEntry[] entryArray = array as DictionaryEntry[];
                Entry[] entries = this.entries;
                for (int i = 0; i < this.count; i++)
                {
                    if (entries[i].hashCode >= 0)
                    {
                        entryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }
            }
            else
            {
                object[] objArray = array as object[];
                if (objArray == null)
                {
                    throw new ArgumentException();
                }
                try
                {
                    int count = this.count;
                    Entry[] entryArray3 = this.entries;
                    for (int j = 0; j < count; j++)
                    {
                        if (entryArray3[j].hashCode >= 0)
                        {
                            objArray[index++] = new KeyValuePair<TKey, TValue>(entryArray3[j].key, entryArray3[j].value);
                        }
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw;
                }
            }
        }

        void IDictionary.Add(object key, object value)
        {
            try
            {
                TKey local = (TKey) key;
                try
                {
                    this.Add(local, (TValue) value);
                }
                catch (InvalidCastException)
                {
                    throw;
                }
            }
            catch (InvalidCastException)
            {
                throw;
            }
        }

        bool IDictionary.Contains(object key)
        {
            return (NullDictionary<TKey, TValue>.IsCompatibleKey(key) && this.ContainsKey((TKey) key));
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator((NullDictionary<TKey, TValue>) this, 1);
        }

        void IDictionary.Remove(object key)
        {
            if (NullDictionary<TKey, TValue>.IsCompatibleKey(key))
            {
                this.Remove((TKey) key);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator((NullDictionary<TKey, TValue>) this, 2);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = this.FindEntry(key);
            if (index >= 0)
            {
                value = this.entries[index].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        // Properties
        public IEqualityComparer<TKey> Comparer
        {
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            get
            {
                return this.comparer;
            }
        }

        public int Count
        {
            get
            {
                return (this.count - this.freeCount);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                int index = this.FindEntry(key);
                if (index >= 0)
                {
                    return this.entries[index].value;
                }
                throw new KeyNotFoundException();
            }
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            set
            {
                this.Insert(key, value, false);
            }
        }

        public KeyCollection Keys
        {
            get
            {
                if (this.keys == null)
                {
                    this.keys = new KeyCollection((NullDictionary<TKey, TValue>) this);
                }
                return this.keys;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                if (this.keys == null)
                {
                    this.keys = new KeyCollection((NullDictionary<TKey, TValue>) this);
                }
                return this.keys;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                if (this.values == null)
                {
                    this.values = new ValueCollection((NullDictionary<TKey, TValue>) this);
                }
                return this.values;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
                }
                return this._syncRoot;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (NullDictionary<TKey, TValue>.IsCompatibleKey(key))
                {
                    int index = this.FindEntry((TKey) key);
                    if (index >= 0)
                    {
                        return this.entries[index].value;
                    }
                }
                return null;
            }
            set
            {
                try
                {
                    TKey local = (TKey) key;
                    try
                    {
                        this[local] = (TValue) value;
                    }
                    catch (InvalidCastException)
                    {
                        throw;
                    }
                }
                catch (InvalidCastException)
                {
                    throw;
                }
            }
        }

        ICollection IDictionary.Keys
        {
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            get
            {
                return this.Keys;
            }
        }

        ICollection IDictionary.Values
        {
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            get
            {
                return this.Values;
            }
        }

        public ValueCollection Values
        {
            get
            {
                if (this.values == null)
                {
                    this.values = new ValueCollection((NullDictionary<TKey, TValue>) this);
                }
                return this.values;
            }
        }

        // Nested Types
        [StructLayout(LayoutKind.Sequential)]
        private struct Entry
        {
            public int hashCode;
            public int next;
            public TKey key;
            public TValue value;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IDictionaryEnumerator, IEnumerator
        {
            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;
            private NullDictionary<TKey, TValue> dictionary;
            private int version;
            private int index;
            private KeyValuePair<TKey, TValue> current;
            private int getEnumeratorRetType;
            internal Enumerator(NullDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                this.version = dictionary.version;
                this.index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                this.current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                if (this.version != this.dictionary.version)
                {
                    throw new InvalidOperationException();
                }
                while (this.index < this.dictionary.count)
                {
                    if (this.dictionary.entries[this.index].hashCode >= 0)
                    {
                        this.current = new KeyValuePair<TKey, TValue>(this.dictionary.entries[this.index].key, this.dictionary.entries[this.index].value);
                        this.index++;
                        return true;
                    }
                    this.index++;
                }
                this.index = this.dictionary.count + 1;
                this.current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
                get
                {
                    return this.current;
                }
            }
            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                    if (this.getEnumeratorRetType == 1)
                    {
                        return new DictionaryEntry(this.current.Key, this.current.Value);
                    }
                    return new KeyValuePair<TKey, TValue>(this.current.Key, this.current.Value);
                }
            }
            void IEnumerator.Reset()
            {
                if (this.version != this.dictionary.version)
                {
                    throw new InvalidOperationException();
                }
                this.index = 0;
                this.current = new KeyValuePair<TKey, TValue>();
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                    return new DictionaryEntry(this.current.Key, this.current.Value);
                }
            }
            object IDictionaryEnumerator.Key
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                    return this.current.Key;
                }
            }
            object IDictionaryEnumerator.Value
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                    return this.current.Value;
                }
            }
        }

        [Serializable, DebuggerTypeProxy(typeof(NullDictionary<,>.KeyCollection.DebugView)), DebuggerDisplay("Count = {Count}")]
        public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, ICollection, IEnumerable
        {
            // Fields
            private NullDictionary<TKey, TValue> dictionary;

            // Methods
            public KeyCollection(NullDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < this.dictionary.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                int count = this.dictionary.count;
                NullDictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0)
                    {
                        array[index++] = entries[i].key;
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return this.dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if (array.Rank != 1)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < this.dictionary.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                TKey[] localArray = array as TKey[];
                if (localArray != null)
                {
                    this.CopyTo(localArray, index);
                }
                else
                {
                    object[] objArray = array as object[];
                    if (objArray == null)
                    {
                        throw new ArgumentException();
                    }
                    int count = this.dictionary.count;
                    NullDictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0)
                            {
                                objArray[index++] = entries[i].key;
                            }
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            // Properties
            public int Count
            {
                get
                {
                    return this.dictionary.Count;
                }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return ((ICollection) this.dictionary).SyncRoot;
                }
            }

            // Nested Types
            [Serializable, StructLayout(LayoutKind.Sequential)]
            public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
            {
                private NullDictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TKey currentKey;
                internal Enumerator(NullDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    this.version = dictionary.version;
                    this.index = 0;
                    this.currentKey = default(TKey);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (this.version != this.dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    while (this.index < this.dictionary.count)
                    {
                        if (this.dictionary.entries[this.index].hashCode >= 0)
                        {
                            this.currentKey = this.dictionary.entries[this.index].key;
                            this.index++;
                            return true;
                        }
                        this.index++;
                    }
                    this.index = this.dictionary.count + 1;
                    this.currentKey = default(TKey);
                    return false;
                }

                public TKey Current
                {
                    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
                    get
                    {
                        return this.currentKey;
                    }
                }
                object IEnumerator.Current
                {
                    get
                    {
                        if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                        {
                            throw new InvalidOperationException();
                        }
                        return this.currentKey;
                    }
                }
                void IEnumerator.Reset()
                {
                    if (this.version != this.dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    this.index = 0;
                    this.currentKey = default(TKey);
                }
            }

            internal sealed class DebugView
            {
                // Fields
                private ICollection<TKey> collection;

                // Methods
                public DebugView(ICollection<TKey> collection)
                {
                    if (collection == null)
                        throw new ArgumentNullException("collection");
                    this.collection = collection;
                }

                // Properties
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public TKey[] Items
                {
                    get
                    {
                        TKey[] array = new TKey[this.collection.Count];
                        this.collection.CopyTo(array, 0);
                        return array;
                    }
                }
            }
        }

        [Serializable, DebuggerTypeProxy(typeof(NullDictionary<,>.ValueCollection.DebugView)), DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, ICollection, IEnumerable
        {
            // Fields
            private NullDictionary<TKey, TValue> dictionary;

            // Methods
            public ValueCollection(NullDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < this.dictionary.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                int count = this.dictionary.count;
                NullDictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0)
                    {
                        array[index++] = entries[i].value;
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return this.dictionary.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if (array.Rank != 1)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((index < 0) || (index > array.Length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if ((array.Length - index) < this.dictionary.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                TValue[] localArray = array as TValue[];
                if (localArray != null)
                {
                    this.CopyTo(localArray, index);
                }
                else
                {
                    object[] objArray = array as object[];
                    if (objArray == null)
                    {
                        throw new ArgumentException("Invalid array type");
                    }
                    int count = this.dictionary.count;
                    NullDictionary<TKey, TValue>.Entry[] entries = this.dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0)
                            {
                                objArray[index++] = entries[i].value;
                            }
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this.dictionary);
            }

            // Properties
            public int Count
            {
                get
                {
                    return this.dictionary.Count;
                }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return ((ICollection) this.dictionary).SyncRoot;
                }
            }

            // Nested Types
            [Serializable, StructLayout(LayoutKind.Sequential)]
            public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
            {
                private NullDictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TValue currentValue;
                internal Enumerator(NullDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    this.version = dictionary.version;
                    this.index = 0;
                    this.currentValue = default(TValue);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (this.version != this.dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    while (this.index < this.dictionary.count)
                    {
                        if (this.dictionary.entries[this.index].hashCode >= 0)
                        {
                            this.currentValue = this.dictionary.entries[this.index].value;
                            this.index++;
                            return true;
                        }
                        this.index++;
                    }
                    this.index = this.dictionary.count + 1;
                    this.currentValue = default(TValue);
                    return false;
                }

                public TValue Current
                {
                    [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
                    get
                    {
                        return this.currentValue;
                    }
                }
                object IEnumerator.Current
                {
                    get
                    {
                        if ((this.index == 0) || (this.index == (this.dictionary.count + 1)))
                        {
                            throw new ArgumentOutOfRangeException("index");
                        }
                        return this.currentValue;
                    }
                }
                void IEnumerator.Reset()
                {
                    if (this.version != this.dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    this.index = 0;
                    this.currentValue = default(TValue);
                }
            }

            internal sealed class DebugView
            {
                // Fields
                private ICollection<TValue> collection;

                // Methods
                public DebugView(ICollection<TValue> collection)
                {
                    if (collection == null)
                        throw new ArgumentNullException();
                    this.collection = collection;
                }

                // Properties
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public TValue[] Items
                {
                    get
                    {
                        TValue[] array = new TValue[this.collection.Count];
                        this.collection.CopyTo(array, 0);
                        return array;
                    }
                }
            }
        }

        internal sealed class DebugView
        {
            // Fields
            private IDictionary<TKey, TValue> dict;

            // Methods
            public DebugView(IDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException("dictionary");
                this.dict = dictionary;
            }

            // Properties
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<TKey, TValue>[] Items
            {
                get
                {
                    KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[this.dict.Count];
                    this.dict.CopyTo(array, 0);
                    return array;
                }
            }
        }
    }

    internal static class HashHelpers
    {
        // Fields
        internal static readonly int[] primes = new int[] { 
            3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3, 0xc5, 0xef, 
            0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b, 0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd, 
            0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f, 0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b, 
            0x1a857, 0x1fd3b, 0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb, 0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f, 
            0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f, 0x4c4987, 0x5b8b6f, 0x6dda89
         };

        // Methods
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentOutOfRangeException("min");
            }
            for (int i = 0; i < primes.Length; i++)
            {
                int num2 = primes[i];
                if (num2 >= min)
                {
                    return num2;
                }
            }
            for (int j = min | 1; j < 0x7fffffff; j += 2)
            {
                if (IsPrime(j))
                {
                    return j;
                }
            }
            return min;
        }

        [SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
            {
                return (candidate == 2);
            }
            int num = (int) Math.Sqrt((double) candidate);
            for (int i = 3; i <= num; i += 2)
            {
                if ((candidate % i) == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
