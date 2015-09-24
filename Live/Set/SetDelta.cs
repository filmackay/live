using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public interface ISetDelta<out T> : ICollectionDelta<T>
    {
    }

    public sealed class SetDelta<T> : CollectionDelta<T, ISetDelta<T>, ISet<T>>, ISetDelta<T>
    {
        private readonly Lazy<HashSet<T>> _inserts = new Lazy<HashSet<T>>();
        private readonly Lazy<HashSet<T>> _deletes = new Lazy<HashSet<T>>();

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

        public override void ApplyTo(ISet<T> target)
        {
            if (Deletes != null)
                foreach (var remove in Deletes)
                    target.Remove(remove);
            if (Inserts != null)
                foreach (var add in Inserts)
                {
                    var added = target.Add(add);

                    // assert if item already existed in set
                    Debug.Assert(added);
                }
        }

        public override void Insert(int index, IEnumerable<T> items)
        {
            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (!_deletes.Value.Remove(item))
                        _inserts.Value.Add(item);
                }
            }
        }

        public override void Update(int index, T oldItem, T newItem)
        {
            using (this.Lock())
            {
                Delete(index, new[] { oldItem });
                Insert(index, new[] { newItem });
            }
        }

        public override void Delete(int index, IEnumerable<T> items)
        {
            using (this.Lock())
            {
                foreach (var item in items)
                {
                    if (!_inserts.Value.Remove(item))
                        _deletes.Value.Add(item);
                }
            }
        }
    }

    public static class SetDelta
    {
        public static SetDelta<TResult> ToSetDelta<TSource, TResult>(this ISetDelta<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> insertSelector, Func<IEnumerable<TSource>, IEnumerable<TResult>> deleteSelector = null)
        {
            if (source == null)
                return null;

            var ret = new SetDelta<TResult>();
            ret.Insert(-1, insertSelector(source.Inserts));
            ret.Delete(-1, (deleteSelector ?? insertSelector)(source.Deletes));
            return ret;
        }

        public static void ApplyTo<T>(this ISetDelta<T> delta, ISet<T> target)
        {
            foreach (var remove in delta.Deletes)
                target.Remove(remove);
            foreach (var add in delta.Inserts)
            {
                var added = target.Add(add);

                // assert if item already existed in set
                Debug.Assert(added);
            }
        }
    }
}
