using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Vertigo.Live
{
    public interface IListDelta<out T> : ICollectionDelta<T>
    {
        IEnumerable<IIndexNode<IListIndexDelta<T>>> IndexDeltas { get; }
    }

    public sealed class ListDelta<T> : CollectionDelta<T, IListDelta<T>, IList<T>>, IListDelta<T>
    {
        private static readonly IEqualityComparer<T> _equalitycomparer = EqualityComparer<T>.Default;

        internal class IndexDelta : IListIndexDelta<T>
        {
            private readonly Lazy<List<T>> _deleteItems = new Lazy<List<T>>();
            public List<T> DeleteItems
            { get { return _deleteItems.Value; } }
            IEnumerable<T> IListIndexDelta<T>.DeleteItems
            { get { return _deleteItems.Value; } }
            IEnumerable IListIndexDelta.DeleteItems
            { get { return _deleteItems.Value; } }

            private readonly Lazy<List<T>> _insertItems = new Lazy<List<T>>();
            public List<T> InsertItems
            { get { return _insertItems.Value; } }
            IEnumerable<T> IListIndexDelta<T>.InsertItems
            { get { return _insertItems.Value; } }
            IEnumerable IListIndexDelta.InsertItems
            { get { return _insertItems.Value; } }

            public bool IsEmpty
            {
                get { return _deleteItems.Value.Count == 0 && _insertItems.Value.Count == 0; }
            }

            public override string ToString()
            {
                var ret = string.Empty;
                if (DeleteItems.Any())
                    ret += " del " + DeleteItems.DelimeteredList(",");

                if (InsertItems.Any())
                    ret += " ins " + InsertItems.DelimeteredList(",");

                return ret;
            }
        }

        private readonly VirtualList<IndexDelta> _indexDeltas = new VirtualList<IndexDelta>(true);

        public IEnumerable<IIndexNode<IListIndexDelta<T>>> IndexDeltas
        {
            get { return _indexDeltas.Dense.Nodes; }
        }

        public void Clear()
        {
            _indexDeltas.Clear();
        }

        public override void ApplyTo(IList<T> target)
        {
            foreach (var indexDelta in IndexDeltas)
            {
                // verify deletes
                var deleteCount = indexDelta.Data.DeleteItems.Count();
                if (!indexDelta.Data.DeleteItems.SequenceEqual(target.Skip(indexDelta.Index).Take(deleteCount)))
                    Debug.Assert(indexDelta.Data.DeleteItems.SequenceEqual(target.Skip(indexDelta.Index).Take(deleteCount)));

                target.RemoveRange(indexDelta.Index, deleteCount);
                target.InsertRange(indexDelta.Index, indexDelta.Data.InsertItems);
            }
        }

        public void Add(T item, int index)
        {
            Insert(index, new[] {item});
        }

        public void Remove(T item, int index)
        {
            Delete(index, new[] {item});
        }

        public override void Add(IListDelta<T> delta)
        {
            if (delta == null)
                return;
            foreach (var indexDelta in delta.IndexDeltas)
            {
                Delete(indexDelta.Index, indexDelta.Data.DeleteItems);
                Insert(indexDelta.Index, indexDelta.Data.InsertItems);
            }
        }

        private VirtualList<IndexDelta>.Node CreateNode(int index, IndexDelta indexDelta = null)
        {
            indexDelta = indexDelta ?? new IndexDelta();
            return _indexDeltas.InsertNode(index, indexDelta.InsertItems.Count - indexDelta.DeleteItems.Count, indexDelta);
        }

        private VirtualList<IndexDelta>.Node GetNode(int index)
        {
            // is there a node covering this index?
            var node = _indexDeltas.Root.FindNearestTo(Side.Left, index);
            if (node != null && index > (node.Index + node.Data.InsertItems.Count)) // leeway of one to ensure prior node is expanded in preference to creating new node
                node = null;
            return node;
        }

        public override void Insert(int index, IEnumerable<T> newItemsEum)
        {
            var newItems = newItemsEum.ToList();

            // get node covering this index
            var node = GetNode(index);
            while (newItems.Count > 0)
            {
                if (node == null)
                    node = CreateNode(index);

                // look for offset matches between existing deletes and new inserts
                var subIndex = index - node.Index;
                var match = FirstMatch(newItems, node.Data.DeleteItems);
                if (match != null)
                {
                    // split node
                    var insertIndex = match.Item1;
                    var deleteIndex = match.Item2;
                    var offsetCount = match.Item3;

                    // split IndexDelta into two
                    var postDelta = new IndexDelta();
                    postDelta.InsertItems.AddRange(node.Data.InsertItems.Skip(subIndex));
                    postDelta.DeleteItems.AddRange(node.Data.DeleteItems.Skip(deleteIndex + offsetCount));

                    // remove offsets / split, and add pre-match inserts
                    node.Data.DeleteItems.TruncateCount(deleteIndex);
                    node.Data.InsertItems.TruncateCount(subIndex);
                    node.Data.InsertItems.AddRange(newItems.Take(insertIndex));
                    newItems.RemoveRange(0, insertIndex + offsetCount);

                    // canonicalize pre node
                    node = CanonicalizeNode(node);

                    // add split IndexDelta
                    index += offsetCount;
                    if (!postDelta.IsEmpty)
                    {
                        // create post node
                        node = _indexDeltas.InsertNode(index, offsetCount, postDelta);
                    }
                }
                else
                {
                    // no deletes to offset - just dump all the inserts here
                    _indexDeltas.AdjustIndex(index + 1, newItems.Count, 0);
                    node.Data.InsertItems.InsertRange(subIndex, newItems);
                    break;
                }

                node.VerifyCanonical();
            }
        }

        public override void Update(int index, T oldItem, T newItem)
        {
            if (oldItem.Equals(newItem))
                return;

            Delete(index, new[] { oldItem });
            Insert(index, new[] { newItem });
        }

        public override void Delete(int index, IEnumerable<T> oldItemsEnum)
        {
            var oldItems = oldItemsEnum.ToList();

            // get node covering this index
            var node = GetNode(index);
            while (oldItems.Count > 0)
            {
                if (node == null)
                    node = CreateNode(index);

                // offset against inserts
                var subIndex = index - node.Index;
                var offsetCount = Math.Min(Math.Max(0, node.Data.InsertItems.Count - subIndex), oldItems.Count);
                if (offsetCount > 0)
                {
                    // check deletes are correct
                    if (!node.Data.InsertItems.Skip(subIndex).Take(offsetCount).SequenceEqual(oldItems.Take(offsetCount)))
                        Debug.Assert(node.Data.InsertItems.Skip(subIndex).Take(offsetCount).SequenceEqual(oldItems.Take(offsetCount)));

                    // remove inserts to offset deletes
                    node.Data.InsertItems.RemoveRange(subIndex, offsetCount);
                    oldItems.RemoveRange(0, offsetCount);
                    _indexDeltas.AdjustIndex(index + 1, -offsetCount, 0);
                }

                // delete up to next node
                var removeCount = (node.Next != null)
                    ? Math.Min(oldItems.Count, node.Next.Index - index)
                    : oldItems.Count;
                if (removeCount > 0)
                {
                    node.Data.DeleteItems.AddRange(oldItems.Take(removeCount));
                    oldItems.RemoveRange(0, removeCount);
                    _indexDeltas.AdjustIndex(index + 1, -removeCount, 0);
                }

                // canonicalize everything
                node = CanonicalizeNode(node);
            }
        }

        private VirtualList<IndexDelta>.Node CanonicalizeNode(VirtualList<IndexDelta>.Node node)
        {
            // does next node exactly line up?
            if (node.Next != null && node.Index + node.Data.InsertItems.Count == node.Next.Index)
            {
                // merge next with this node
                node.Data.DeleteItems.AddRange(node.Next.Data.DeleteItems);
                node.Data.InsertItems.AddRange(node.Next.Data.InsertItems);

                // remove next node but do not adjust index since we are just redistributing
                _indexDeltas.Remove(node.Next);
                _indexDeltas.AdjustIndex(node.Index + 1, 0, -1);
            }

            if (node.Data.IsEmpty)
            {
                // adjust the dense index, if next node is not a duplicate
                var next = node.Next;

                // remove the node
                _indexDeltas.Remove(node);
                _indexDeltas.AdjustIndex(node.Index, 0, -1);
                node = next;
            }

            // verify
            node.VerifyCanonical();

            return node;
        }

        private static Tuple<int, int, int> FirstMatch(IList<T> findThis, IList<T> inThis)
        {
            // source2 is bigger
            for (var x1 = 0; x1 < findThis.Count; x1++)
            {
                var x2 = inThis.IndexOf(findThis[x1]);
                if (x2 != -1)
                {
                    // count matches
                    var count = 1;
                    var maxCount = Math.Min(findThis.Count - x1 - 1, inThis.Count - x2 - 1);
                    while (count < maxCount && _equalitycomparer.Equals(findThis[x1 + count], inThis[x2 + count]))
                        count++;
                    return Tuple.Create(x1, x2, count);
                }
            }

            return null;
        }

        public override IEnumerable<T> Inserts
        {
            get
            {
                if (_indexDeltas.Count == 0)
                    return null;
                var ret = _indexDeltas.Dense.SelectMany(delta => delta.InsertItems).ToArray();
                if (ret.Length == 0)
                    return null;
                return ret;
            }
        }

        public override IEnumerable<T> Deletes
        {
            get
            {
                if (_indexDeltas.Count == 0)
                    return null;
                var ret = _indexDeltas.Dense.SelectMany(delta => delta.DeleteItems).ToArray();
                if (ret.Length == 0)
                    return null;
                return ret;
            }
        }

        public override string ToString()
        {
            return string.Join("; ", IndexDeltas.Select(id => string.Format("#{0}:{1}", id.Index, id.Data)));
        }
    }

    public static partial class Extensions
    {
        public static bool Equals<T>(this IListDelta<T> delta1, IListDelta<T> delta2)
        {
            if (delta1 == null && delta2 == null)
                return true;
            if (delta1 == null || delta2 == null)
                return false;
            return delta1.IndexDeltas.SequenceEqual(delta2.IndexDeltas, new IndexNodeComparer<T>());
        }

        public static ListDelta<TResult> ToListDelta<TSource, TResult>(this IListDelta<TSource> source, Action<ListDelta<TResult>, IEnumerable<IIndexNode<IListIndexDelta<TSource>>>> applyChanges)
        {
            if (source == null)
                return null;

            var delta = new ListDelta<TResult>();
            applyChanges(delta, source.IndexDeltas);

            return delta;
        }

        public static ListDelta<TResult> ToListDelta<TSource, TResult>(this IListDelta<TSource> source, Func<TSource, TResult> selector, Func<TSource, bool> filter = null)
        {
            return source.ToListDelta<TSource, TResult>((newDelta, changes) =>
                {
                    // prepare transformation function
                    var transform =
                        new Func<IEnumerable<TSource>, IEnumerable<TResult>>
                            (s =>
                                {
                                    if (filter != null)
                                        s = s.Where(filter);
                                    return s.Select(selector);
                                }
                            );

                    // apply changes
                    foreach (var change in changes)
                    {
                        // apply deletes
                        var deletes = transform(change.Data.DeleteItems).ToArray();
                        if (deletes.Length > 0)
                            newDelta.Delete(change.Index, deletes);

                        // apply insert
                        var inserts = transform(change.Data.InsertItems).ToArray();
                        if (inserts.Length > 0)
                            newDelta.Insert(change.Index, inserts);
                    }
                });
        }

        public static IIndexNode<IListIndexDelta<T>> SplitAt<T>(this IIndexNode<IListIndexDelta<T>> source, int index)
        {
            if (source.Data.InsertItems.Count() <= index && source.Data.DeleteItems.Count() <= index)
                return source;

            // create pre node
            var preDelta = new ListDelta<T>.IndexDelta();
            preDelta.InsertItems.AddRange(source.Data.InsertItems.Take(index));
            preDelta.DeleteItems.AddRange(source.Data.DeleteItems.Take(index));
            var preNode = new AnonymousIndexNode<IListIndexDelta<T>>
                {
                    Data = preDelta,
                    Previous = source.Previous,
                    Index = source.Index
                };

            // create post node
            var postDelta = new ListDelta<T>.IndexDelta();
            postDelta.InsertItems.AddRange(source.Data.InsertItems.Skip(index));
            postDelta.DeleteItems.AddRange(source.Data.DeleteItems.Skip(index));
            var postNode = new AnonymousIndexNode<IListIndexDelta<T>>
                {
                    Data = postDelta,
                    Next = source.Next,
                    Index = source.Index + index
                };

            // link nodes together
            preNode.Next = postNode;
            postNode.Previous = preNode;

            return preNode;
        }

        internal static bool ContainsIndex<T>(this VirtualList<ListDelta<T>.IndexDelta>.Node node, int index)
        {
            return node != null &&
                   index >= node.Index &&
                   index <= (node.Index + node.Data.InsertItems.Count);
        }

        internal static void VerifyCanonical<T>(this VirtualList<ListDelta<T>.IndexDelta>.Node node)
        {
#if DEBUG
            if (node == null)
                return;

            if (node.Previous != null)
            {
                // should be merged with Previous?
                if (node.Previous.Index + node.Previous.Data.InsertItems.Count >= node.Index)
                    Debug.Print("Previous node can be merged with node");
            }

            if (node.Next != null)
            {
                // should be merged with Next?
                if (node.Index + node.Data.InsertItems.Count >= node.Next.Index)
                    throw new InvalidOperationException("Next node can be merged with node");
            }

            if (node.Data.IsEmpty)
                throw new InvalidOperationException("Previous node can be merged with node");
#endif
        }
    }
}
