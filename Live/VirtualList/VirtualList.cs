using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace Vertigo.Live
{
    public partial class VirtualList<T> : IList<T>
    {
        internal static int Level(Node node)
        {
            return node == null ? 0 : node._level;
        }

        private Node _root;
        public Node Root
        {
            get { return _root; }
            private set
            {
                if (_root == value)
                    return;

                _root = value;

                if (_root != null)
                    _root.Parent = null;
            }
        }
        private readonly MultiMap<T, Node> _multiNodeByValue;
        private readonly Dictionary<T, Node> _uniqueNodeByValue;
        private readonly IDictionary<T, Node> _nodeByValue;

        public VirtualList(bool uniqueItems)
        {
            _dense = new DenseList(this);
            _nodeByValue = uniqueItems
                ? (IDictionary<T, Node>)(_uniqueNodeByValue = new Dictionary<T, Node>())
                : _multiNodeByValue = new MultiMap<T, Node>();
        }

        public VirtualList(bool uniqueItems, IEnumerable<T> source)
            : this(uniqueItems)
        {
            source.ForEach(Add);
        }

        private static Node Skew(Node node)
        {
            if (Level(node) == Level(node.Left()))
            {
                // rotate right
                Node left = node.Left();
                if (node != null)
                    node.Left = left.Right;
                if (left != null)
                    left.Right = node;
                node = left;
            }
            return node;
        }

        private static Node Split(Node node)
        {
            //if (node == null)
            //    return null;
            if (Level(node.Right().Right()) == Level(node))
            {
                // rotate left
                Node right = node.Right;
                node.Right = right.Left;
                right.Left = node;
                node = right;
                node._level++;
            }
            return node;
        }

        private static Node Insert(Node parent, Node node, int index, T value, out Node newNode)
        {
            if (node == null)
            {
                // adjust dense index depending on whether we are to left or right of parent
                var denseIndex =
                    parent == null
                        ? 0
                        : parent.DenseIndex + (index < parent.Index ? -1 : +1);

                // create new node
                return newNode = new Node(index, denseIndex, value);
            }

            if (index < node.Index)
            {
                node.Left = Insert(node, node.Left, index, value, out newNode);
                if (newNode == null)
                    return node;
            }
            else if (index > node.Index)
            {
                node.Right = Insert(node, node.Right, index, value, out newNode);
                if (newNode == null)
                    return node;
            }
            else // (index == node.Index)
            {
                // duplicate!
                newNode = null;
                return node;
            }

            node = Skew(node);
            node = Split(node);

            return node;
        }

        private void Update(Node node, T value)
        {
            // remove from lookup
            if (_uniqueNodeByValue != null)
                _uniqueNodeByValue.Remove(node.Data);
            else
                _multiNodeByValue.Remove(node.Data, node);

            // update node
            node.Data = value;

            // add to lookup
            _nodeByValue.Add(value, node);
        }

        private static Node Delete(Node parent, Node node, int index, bool denseIndex, ref Node target, ref Node replacement)
        {
            var newNode = node;
            if (newNode == null)
            {
                // found replacement node?
                if (target != null && replacement == null)
                    replacement = target == parent ? null : parent; // (dont use parent in the case where it is the target)
                return newNode;
            }

            int compare = index.CompareTo(denseIndex ? newNode.DenseIndex : newNode.Index);
            if (compare == -1)
            {
                newNode.Left = Delete(newNode, newNode.Left, index, denseIndex, ref target, ref replacement);
                if (target == null)
                    return newNode;
            }
            else if (compare == 1)
            {
                newNode.Right = Delete(newNode, newNode.Right, index, denseIndex, ref target, ref replacement);
                if (target == null)
                    return newNode;
            }
            else // compare == 0
            {
                // found target
                target = newNode;
                var newRight = Delete(newNode, newNode.Right, index, denseIndex, ref target, ref replacement);
                if (target == null)
                    return newNode;

                if (replacement == null)
                    // nothing to replace 'node' with
                    newNode = newRight;
                else
                {
                    // replace 'node' with 'replacement'
                    Debug.Assert(replacement.Left == null);
                    replacement.Left = newNode.Left;
                    Debug.Assert(replacement.Right == null || replacement.Right == newRight);
                    replacement.Right = newRight;
                    replacement._level = node._level;
                    newNode = replacement;
                }
            }

            if (replacement == node)
            {
                // remove 'replacement'
                if (newNode != null)
                    newNode = newNode.Right;  // effectively removes 'node' from tree
            }
            else if ((Level(newNode.Left()) < newNode.Level() - 1) ||
                      (Level(newNode.Right()) < newNode.Level() - 1))
            {
                // rebalance tree
                --newNode._level;
                if (Level(newNode.Right) > newNode._level)
                    if (newNode.Right != null)
                        newNode.Right._level = newNode._level;
                newNode = Skew(newNode);
                newNode.Right = Skew(newNode.Right);
                if (newNode.Right != null)
                    newNode.Right.Right = Skew(newNode.Right.Right);
                newNode = Split(newNode);
                newNode.Right = Split(newNode.Right);
            }

            return newNode;
        }

        public void AdjustIndex(int startIndex, int adjustment, int denseAdjustment = 0)
        {
            if (Root != null)
                Root.AdjustIndex(Root.Index, startIndex, adjustment, denseAdjustment, false);
        }

        public void Insert(int index, T value)
        {
            InsertNode(index, +1, value);
        }

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public int Count
        {
            get
            {
                var last = Root.Last();
                return last == null ? 0 : last.Index + 1;
            }
        }

        public void Clear()
        {
            _nodeByValue.Clear();
            Root = null;
        }

        public bool Contains(T item)
        {
            return _nodeByValue.ContainsKey(item);
        }

        public Node NodeOf(T item)
        {
            // use hash table
            Node node;
            _nodeByValue.TryGetValue(item, out node);
            return node;
        }

        public int IndexOf(T item)
        {
            var node = NodeOf(item);
            return node == null ? -1 : node.Index;
        }

        public bool Remove(T item)
        {
            // use hash table
            var index = IndexOf(item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        public void Remove(Node node)
        {
            RemoveGetNode(node.DenseIndex, true);
        }

        public Node RemoveGetNode(int index, bool denseIndex)
        {
            Node deleted = null;
            Node replacement = null;
            Root = Delete(null, Root, index, denseIndex, ref deleted, ref replacement);
            if (deleted != null)
                if (_uniqueNodeByValue != null)
                    _uniqueNodeByValue.Remove(deleted.Data);
                else
                    _multiNodeByValue.Remove(deleted.Data, deleted);
            return deleted;
        }

        public void RemoveAt(int index)
        {
            var removedNode = RemoveGetNode(index, false);
            AdjustIndex(index, -1, removedNode == null ? 0 : -1);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        public T this[int index]
        {
            get
            {
                var node = Root.Find(index);
                return node == null ? default(T) : node.Data;
            }

            set
            {
                var node = Root.Find(index);
                if (node == null)
                    Insert(index, value);
                else
                    Update(node, value);
            }
        }

        public Node SetAt(int index, T value)
        {
            var node = Root.Find(index);

            // update existing node?
            if (node != null)
            {
                Update(node, value);
                return node;
            }

            // create new node
            return InsertNode(index, 0, value);
        }

        public Node InsertNode(int index, int indexAdjustment, T value)
        {
            // make room in index
            AdjustIndex(index, indexAdjustment, +1);

            // create new node
            Node newNode;
            Root = Insert(null, Root, index, value, out newNode);
            _nodeByValue.Add(value, newNode);
            return newNode;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Nodes
                .Select(node => node.Data)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public IEnumerable<Node> Nodes
        {
            get
            {
                var index = 0;
                foreach (var node in Dense.Nodes)
                {
                    // padding with default values
                    var padding = node.Index - index;
                    if (padding > 0)
                    {
                        for (var i = 0; i < padding; i++)
                            yield return new Node(index + i, -1, default(T));
                        index += padding;
                    }

                    // return sparse node
                    yield return node;
                    index++;
                }
            }
        }

        private readonly DenseList _dense;
        public DenseList Dense
        {
            get { return _dense; }
        }

        public Node NodeAt(int index)
        {
            return Root.Find(index);
        }

        public int IndexOfDenseIndex(int denseIndex)
        {
            var node = Dense.NodeAt(denseIndex);
            return node == null ? -1 : node.Index;
        }

        public int DenseIndexOfIndex(int index)
        {
            var node = Root.FindNearestTo(Side.Left, index);

            // prior to first (dense) element
            if (node == null)
                return 0;

            // perfect match?
            if (node.Index == index)
                return node.DenseIndex;

            // index would be the (new) last node
            return node.DenseIndex + 1;
        }

        public string Html
        {
            get { return Root == null ? "" : Root.Html; }
        }

        public void Check()
        {
            Nodes.ForEach(node => node.Check());
        }

        public class DenseList : IList<T>
        {
            private readonly VirtualList<T> _parent;

            public DenseList(VirtualList<T> parent)
            {
                _parent = parent;
            }

            public IEnumerable<Node> Nodes
            {
                get { return _parent.Root.DescendendsAndSelf(); }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Nodes
                    .Select(node => node.Data)
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                _parent.Add(item);
            }

            public void Clear()
            {
                _parent.Clear();
            }

            public bool Contains(T item)
            {
                return _parent.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                foreach (var item in this)
                    array[arrayIndex++] = item;
            }

            public bool Remove(T item)
            {
                return _parent.Remove(item);
            }

            public int Count
            {
                get { return _parent._nodeByValue.Count; }
            }

            public bool IsReadOnly
            {
                get { return _parent.IsReadOnly; }
            }

            public int IndexOf(T item)
            {
                // use hash table
                Node node = _parent.NodeOf(item);
                return node == null ? -1 : node.DenseIndex;
            }

            public void Insert(int index, T item)
            {
                // we dont know what the sparse index is
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                _parent.RemoveAt(_parent.IndexOfDenseIndex(index));
            }

            public T this[int denseIndex]
            {
                get { return _parent[_parent.IndexOfDenseIndex(denseIndex)]; }
                set { _parent[_parent.IndexOfDenseIndex(denseIndex)] = value; }
            }

            public Node NodeAt(int denseIndex)
            {
                return _parent.Root.FindByDenseIndex(denseIndex);
            }
        }
    }
}
