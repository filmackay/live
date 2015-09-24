using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Vertigo.Live
{
    // dont think we really need this as there is one in System.Windows.ObjectModel
    [Serializable, DebuggerDisplay("Count = {Count}")]
    public class Collection<T> : ICollection<T>, ICollection, IEquatable<IEnumerable<T>>
    {
        // Fields
        private const int _defaultCapacity = 4;
        private static T[] _emptyArray;
        private T[] _items;
        private int _size;
        [NonSerialized]
        private object _syncRoot;
        private int _version;
        private static IEqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

        // Methods
        static Collection()
        {
            _emptyArray = new T[0];
        }

        public Collection()
        {
            _items = _emptyArray;
        }

        public Collection(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException();

            var count = collection.Count();
            _items = new T[count];

            AddRange(collection);
        }

        public Collection(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            this._items = new T[capacity];
        }

        public void Add(T item)
        {
            // insertion sort
            var index = Array.BinarySearch(_items, 0, _size, item, HashComparer<T>.Default);
            index = index >= 0 ? index : ~index;
            Insert(index, item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            EnsureCapacity(Count + collection.Count());
            collection.ForEach(Add);
        }

        public void Clear()
        {
            if (this._size > 0)
            {
                Array.Clear(this._items, 0, this._size);
                this._size = 0;
            }
            this._version++;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array)
        {
            this.CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this._items, 0, array, arrayIndex, this._size);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if ((this._size - index) < count)
            {
                throw new ArgumentOutOfRangeException();
            }
            Array.Copy(this._items, index, array, arrayIndex, count);
        }

        private void EnsureCapacity(int min)
        {
            if (this._items.Length < min)
            {
                int num = (this._items.Length == 0) ? 4 : (this._items.Length * 2);
                if (num < min)
                {
                    num = min;
                }
                this.Capacity = num;
            }
        }

        public bool Exists(Predicate<T> match)
        {
            return (this.FindIndex(match) != -1);
        }

        public T Find(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            for (int i = 0; i < this._size; i++)
            {
                if (match(this._items[i]))
                {
                    return this._items[i];
                }
            }
            return default(T);
        }

        public Collection<T> FindAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            Collection<T> list = new Collection<T>();
            for (int i = 0; i < this._size; i++)
            {
                if (match(this._items[i]))
                {
                    list.Add(this._items[i]);
                }
            }
            return list;
        }

        private int FindIndex(Predicate<T> match)
        {
            return this.FindIndex(0, this._size, match);
        }

        private int FindIndex(int startIndex, Predicate<T> match)
        {
            return this.FindIndex(startIndex, this._size - startIndex, match);
        }

        private int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if (startIndex > this._size)
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((count < 0) || (startIndex > (this._size - count)))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            int num = startIndex + count;
            for (int i = startIndex; i < num; i++)
            {
                if (match(this._items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            for (int i = this._size - 1; i >= 0; i--)
            {
                if (match(this._items[i]))
                {
                    return this._items[i];
                }
            }
            return default(T);
        }

        private int FindLastIndex(Predicate<T> match)
        {
            return this.FindLastIndex(this._size - 1, this._size, match);
        }

        private int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return this.FindLastIndex(startIndex, startIndex + 1, match);
        }

        private int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            if (this._size == 0)
            {
                if (startIndex != -1)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            else if (startIndex >= this._size)
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((count < 0) || (((startIndex - count) + 1) < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            int num = startIndex - count;
            for (int i = startIndex; i > num; i--)
            {
                if (match(this._items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException();
            }
            for (int i = 0; i < this._size; i++)
            {
                action(this._items[i]);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        private int IndexOf(T item)
        {
            // check items are sorted correctly
            Debug.Assert(_items.Take(_size).SequenceEqual(_items.Take(_size).OrderBy(i => _equalityComparer.GetHashCode(i))));

            // any hash matches?
            var index = Array.BinarySearch(_items, 0, _size, item, HashComparer<T>.Default);
            if (index < 0)
                return -1;

            // go to first hash match
            var hashCode = _equalityComparer.GetHashCode(item);
            while (index > 0 && _equalityComparer.GetHashCode(_items[index - 1]) == hashCode)
                index--;

            // go through all hash matches
            while (index < _size && _equalityComparer.GetHashCode(_items[index]) == hashCode)
            {
                if (_equalityComparer.Equals(_items[index], item))
                    return index;
                index++;
            }

            // no hash matches matched item
            return -1;
        }

        private void Insert(int index, T item)
        {
            if (index > this._size)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (this._size == this._items.Length)
            {
                this.EnsureCapacity(this._size + 1);
            }
            if (index < this._size)
            {
                Array.Copy(this._items, index, this._items, index + 1, this._size - index);
            }
            this._items[index] = item;
            this._size++;
            this._version++;
        }

        private static bool IsCompatibleObject(object value)
        {
            if (!(value is T) && ((value != null) || typeof(T).IsValueType))
            {
                return false;
            }
            return true;
        }

        private void RemoveAt(int index)
        {
            if (index >= this._size)
            {
                throw new ArgumentOutOfRangeException();
            }
            this._size--;
            if (index < this._size)
            {
                Array.Copy(this._items, index + 1, this._items, index, this._size - index);
            }
            this._items[this._size] = default(T);
            this._version++;
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
                return false;


            RemoveAt(index);
            return true;
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            int index = 0;
            while ((index < this._size) && !match(this._items[index]))
            {
                index++;
            }
            if (index >= this._size)
            {
                return 0;
            }
            int num2 = index + 1;
            while (num2 < this._size)
            {
                while ((num2 < this._size) && match(this._items[num2]))
                {
                    num2++;
                }
                if (num2 < this._size)
                {
                    this._items[index++] = this._items[num2++];
                }
            }
            Array.Clear(this._items, index, this._size - index);
            int num3 = this._size - index;
            this._size = index;
            this._version++;
            return num3;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                throw new ArgumentOutOfRangeException();
            }
            try
            {
                Array.Copy(this._items, 0, array, arrayIndex, this._size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public T[] ToArray()
        {
            T[] destinationArray = new T[this._size];
            Array.Copy(this._items, 0, destinationArray, 0, this._size);
            return destinationArray;
        }

        public void TrimExcess()
        {
            var num = (int)(this._items.Length * 0.9);
            if (this._size < num)
            {
                this.Capacity = this._size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException();
            }
            for (int i = 0; i < this._size; i++)
            {
                if (!match(this._items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static void VerifyValueType(object value)
        {
            if (!Collection<T>.IsCompatibleObject(value))
            {
                throw new InvalidDataException();
            }
        }

        // Properties
        public int Capacity
        {
            get
            {
                return this._items.Length;
            }
            set
            {
                if (value != this._items.Length)
                {
                    if (value < this._size)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    if (value > 0)
                    {
                        T[] destinationArray = new T[value];
                        if (this._size > 0)
                        {
                            Array.Copy(this._items, 0, destinationArray, 0, this._size);
                        }
                        this._items = destinationArray;
                    }
                    else
                    {
                        this._items = Collection<T>._emptyArray;
                    }
                }
            }
        }

        public int Count
        {
            get { return _size; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        
        public bool Equals(IEnumerable<T> other)
        {
            return this.UnorderedEqual(other);
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    Interlocked.CompareExchange(ref this._syncRoot, new object(), null);
                }
                return this._syncRoot;
            }
        }

        // Nested Types
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private Collection<T> list;
            private int index;
            private int version;
            private T current;
            internal Enumerator(Collection<T> list)
            {
                this.list = list;
                this.index = 0;
                this.version = list._version;
                this.current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                Collection<T> list = this.list;
                if ((this.version == list._version) && (this.index < list._size))
                {
                    this.current = list._items[this.index];
                    this.index++;
                    return true;
                }
                return this.MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (this.version != this.list._version)
                {
                    throw new InvalidOperationException("Collection changed while iterating");
                }
                this.index = this.list._size + 1;
                this.current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.list._size + 1)))
                    {
                        throw new InvalidOperationException();
                    }
                    return this.Current;
                }
            }
            void IEnumerator.Reset()
            {
                if (this.version != this.list._version)
                {
                    throw new InvalidOperationException("Collection changed while iterating");
                }
                this.index = 0;
                this.current = default(T);
            }
        }
    }

    public static partial class Extensions
    {
        public static Collection<T> ToCollection<T>(this IEnumerable<T> source)
        {
            return new Collection<T>(source);
        }
    }
}
