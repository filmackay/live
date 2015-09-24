using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public interface ICollectionDelta<out T>
    {
        IEnumerable<T> Inserts { get; } // should return null if there are no items
        IEnumerable<T> Deletes { get; } // should return null if there are no items
    }

    public abstract class CollectionDelta<T, TIDelta, TICollection> : ICollectionDelta<T>
        where TIDelta : ICollectionDelta<T>
        where TICollection : ICollection<T>
    {
        public abstract IEnumerable<T> Inserts { get; }
        public abstract IEnumerable<T> Deletes { get; }

        // mutable methods:
        public abstract void Insert(int index, IEnumerable<T> items);
        public abstract void Update(int index, T oldItem, T newItem);
        public abstract void Delete(int index, IEnumerable<T> items);

        public virtual void Add(TIDelta delta)
        {
            if (!delta.HasChange())
                return;

            using (this.Lock())
            {
                if (delta.Deletes != null)
                    Delete(-1, delta.Deletes);
                if (delta.Inserts != null)
                    Insert(-1, delta.Inserts);
            }
        }

        public virtual void ApplyTo(TICollection target)
        {
            if (Deletes != null)
                foreach (var remove in Deletes)
                    target.Remove(remove);
            if (Inserts != null)
                foreach (var add in Inserts)
                    target.Add(add);
        }

        public override string ToString()
        {
            var ret = string.Empty;
            if (Deletes != null)
                ret += " del " + string.Join(",", Deletes.Select(i => i.ToString()));

            if (Inserts != null)
                ret += " ins " + string.Join(",", Inserts.Select(i => i.ToString()));

            return ret;
        }

        public static implicit operator TIDelta(CollectionDelta<T, TIDelta, TICollection> delta)
        {
            return (TIDelta)(object)delta;
        }
    }

    public sealed class CollectionDelta<T> : CollectionDelta<T, ICollectionDelta<T>, ICollection<T>>
    {
        private readonly Lazy<Collection<T>> _inserts = new Lazy<Collection<T>>();
        private readonly Lazy<Collection<T>> _deletes = new Lazy<Collection<T>>();

        public override IEnumerable<T> Inserts
        {
            get
            {
                return
                    _inserts.IsValueCreated && _inserts.Value.Count > 0
                        ? _inserts.Value
                        : null;
            }
        }

        public override IEnumerable<T> Deletes
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
            using (this.Lock())
            {
                if (_inserts.IsValueCreated)
                    _inserts.Value.Clear();
                if (_deletes.IsValueCreated)
                    _deletes.Value.Clear();
            }
        }

        public override void Insert(int index, IEnumerable<T> items)
        {
            Debug.Assert(index == -1);

            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (!_deletes.IsValueCreated || !_deletes.Value.Remove(item))
                        _inserts.Value.Add(item);
                }
            }
        }

        public override void Update(int index, T oldItem, T newItem)
        {
            Debug.Assert(index == -1);

            using (this.Lock())
            {
                Delete(index, new[] {oldItem});
                Insert(index, new[] {newItem});
            }
        }

        public override void Delete(int index, IEnumerable<T> items)
        {
            Debug.Assert(index == -1);

            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (!_inserts.IsValueCreated || !_inserts.Value.Remove(item))
                        _deletes.Value.Add(item);
                }
            }
        }
    }

    public static class CollectionDelta
    {
        public static CollectionDelta<T, TIDelta, TICollection> ToMutable<T, TIDelta, TICollection>(this TIDelta source)
            where TIDelta : ICollectionDelta<T>
            where TICollection : ICollection<T>
        {
            // already matches?
            var ret = source as CollectionDelta<T, TIDelta, TICollection>;
            if (ret != null)
                return ret;

            var baseType = source.GetType().GetGenericTypeDefinition();
            if (baseType == null)
                throw new InvalidOperationException("Delta is not a single type parameter generic");

            // create copy
            ret = (CollectionDelta<T, TIDelta, TICollection>)Activator.CreateInstance(baseType.MakeGenericType(typeof(T)));
            ret.Add(source);
            return ret;
        }

        public static bool DeltaEquals<T>(this ICollectionDelta<T> delta1, ICollectionDelta<T> delta2)
        {
            if (delta1 == null && delta2 == null)
                return true;
            if (delta1 == null || delta2 == null)
                return false;
            return delta1.Inserts.UnorderedEqual(delta2.Inserts) && delta1.Deletes.UnorderedEqual(delta2.Deletes);
        }

        public static bool HasChange<T>(this ICollectionDelta<T> delta)
        {
            return delta != null && (delta.Inserts != null || delta.Deletes != null);
        }

        public static TIDelta Merge<T, TIDelta, TICollection>(this IList<TIDelta> deltas)
            where TIDelta : ICollectionDelta<T>
            where TICollection : ICollection<T>
        {
            if (deltas.Count == 0)
                return default(TIDelta);

            var ret = deltas[0].ToMutable<T, TIDelta, TICollection>();
            for (var i = 1; i < deltas.Count; i++)
                ret.Add(deltas[i]);
            return ret.HasChange() ? ret : default(TIDelta);
        }

        public static CollectionDelta<TResult> ToCollectionDelta<TSource, TResult>(this ICollectionDelta<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> insertSelector, Func<IEnumerable<TSource>, IEnumerable<TResult>> deleteSelector = null)
        {
            if (!source.HasChange())
                return null;

            var ret = new CollectionDelta<TResult>();
            if (source.Inserts != null)
                ret.Insert(-1, insertSelector(source.Inserts));
            if (source.Deletes != null)
                ret.Delete(-1, (deleteSelector ?? insertSelector)(source.Deletes));
            return ret;
        }

        public static SetDelta<TResult> ToSetDelta<TSource, TResult>(this ICollectionDelta<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> insertSelector, Func<IEnumerable<TSource>, IEnumerable<TResult>> deleteSelector = null)
        {
            if (!source.HasChange())
                return null;

            var ret = new SetDelta<TResult>();
            if (source.Inserts != null)
                ret.Insert(-1, insertSelector(source.Inserts));
            if (source.Deletes != null)
                ret.Delete(-1, (deleteSelector ?? insertSelector)(source.Deletes));
            return ret;
        }

        //public static ListDelta<TResult> ToListDelta<TSource, TResult>(this ICollectionDelta<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> insertSelector, Func<IEnumerable<TSource>, IEnumerable<TResult>> deleteSelector = null)
        //{
        //    if (source == null)
        //        return null;

        //    var ret = new ListDelta<TResult>();
        //    ret.Insert(-1, insertSelector(source.Inserts));
        //    ret.Delete(-1, (deleteSelector ?? insertSelector)(source.Deletes));
        //    return ret;
        //}

        public static DictionaryDelta<TKey, TValue> ToDictionaryDelta<TSource, TKey, TValue>(this ICollectionDelta<TSource> source, Func<IEnumerable<TSource>, IEnumerable<KeyValuePair<TKey, TValue>>> insertSelector, Func<IEnumerable<TSource>, IEnumerable<KeyValuePair<TKey, TValue>>> deleteSelector = null)
        {
            if (!source.HasChange())
                return null;

            var ret = new DictionaryDelta<TKey, TValue>();
            if (source.Inserts != null)
                ret.Insert(-1, insertSelector(source.Inserts));
            if (source.Deletes != null)
                ret.Delete(-1, (deleteSelector ?? insertSelector)(source.Deletes));
            return ret;
        }
    }
}
