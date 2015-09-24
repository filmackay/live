using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace Vertigo.Live
{
    public enum Side
    {
        Left,
        Right,
    }

    public enum ChildType
    {
        Left,
        Right,
        Root,
    }

    public enum ParentType
    {
        Right,
        Both,
        Leaf,
    }

    public interface IIndexNode<out T>
    {
        int DenseIndex { get; }
        int Index { get; }
        T Data { get; }
        IIndexNode<T> Next { get; }
        IIndexNode<T> Previous { get; }
    }

    public partial class VirtualList<T>
    {
        public class Node : IIndexNode<T>
        {
            // user data
            private T _data;
            public int _level;
            private Node _left;
            private Node _right;
            private int _relativeIndex;
            private int _relativeDenseIndex;

            public Node Right
            {
                get { return _right; }
                set { this[Side.Right] = value; }
            }

            public Node Left
            {
                get { return _left; }
                set { this[Side.Left] = value; }
            }

            public Node this[Side side]
            {
                get { return side == Side.Left ? _left : _right; }
                set
                {
                    if (this == value)
                        throw new InvalidOperationException("Node cannot be a parent of itself");

                    // make sure there is a change
                    var old = this[side];
                    if (old == value)
                        return;

                    // detach old
                    if (old != null)
                        old.Parent = null;

                    // detach new
                    if (value != null)
                        value.ParentToThis = null;

                    // attach new
                    if (side == Side.Left)
                        _left = value;
                    else
                        _right = value;
                    if (value != null)
                        value.Parent = this;
                }
            }

            public IEnumerable<Node> Children
            {
                get
                {
                    if (_left != null)
                        yield return _left;
                    if (_right != null)
                        yield return _left;
                }
            }

            public Node DenseSibling(Side direction)
            {
                var node = this;
                if (node[direction] == null)
                {
                    // we have hit a leaf - we need to move back up the tree

                    // there is no right child, move up until we hit a child left of the parent
                    while (node.ChildType == (ChildType)direction)
                        node = node.Parent;
                    node = node.Parent;
                }
                else
                {
                    // there is a right child - move to the minimum child
                    node = node[direction].FarChild(direction.Opposite());
                }

                return node;
            }

            IIndexNode<T> IIndexNode<T>.Next
            {
                get { return Next; }
            }

            IIndexNode<T> IIndexNode<T>.Previous
            {
                get { return Previous; }
            }

            public Node Next
            {
                get { return DenseSibling(Side.Right); }
            }

            public Node Previous
            {
                get { return DenseSibling(Side.Left); }
            }

            private Node _parent;
            public Node Parent
            {
                get { return _parent; }

                internal set
                {
                    Debug.Assert(value != this);
                    if (_parent == value)
                        return;

                    // work out difference in new verses old parent index
                    var indexDiff = Parent.Index() - (value == null ? 0 : value.Index);
                    var denseIndexDiff = Parent.DenseIndex() - (value == null ? 0 : value.DenseIndex);

                    // preserve my absolute index, despite changing parents
                    _relativeIndex += indexDiff;
                    _relativeDenseIndex += denseIndexDiff;

                    // keep new parent
                    _parent = value;
                }
            }

            public int DenseIndex
            {
                get { return Parent.DenseIndex() + _relativeDenseIndex; }
            }

            public void SetIndexes(int index, int denseIndex)
            {
                var diff = index - Index;
                var denseDiff = denseIndex - DenseIndex;
                if (diff != 0 || denseDiff != 0)
                {
                    // adjust our relative index
                    AdjustIndexSimple(diff, denseDiff);

                    // adjust children to keep their index the same - check for nulls since we still may be in the process
                    // of being created and populated with data with no real or sentinal children
                    if (_left != null)
                        _left.AdjustIndexSimple(-diff, -denseDiff);
                    if (_right != null)
                        _right.AdjustIndexSimple(-diff, -denseDiff);
                }
            }

            public virtual void AdjustIndexSimple(int adjustment, int denseAdjustment)
            {
                _relativeIndex += adjustment;
                _relativeDenseIndex += denseAdjustment;
            }

            public virtual void AdjustIndex(int preAdjustmentIndex, int startIndex, int adjustment, int denseAdjustment, bool alreadyAdjusted)
            {
                // this node requires adjustment if the target index is to my left
                var requiresAdjustment = (preAdjustmentIndex >= startIndex);

                // perform any adjustments
                if (requiresAdjustment && !alreadyAdjusted)
                {
                    _relativeIndex += adjustment;
                    _relativeDenseIndex += denseAdjustment;
                }
                else if (!requiresAdjustment && alreadyAdjusted)
                {
                    _relativeIndex -= adjustment;
                    _relativeDenseIndex -= denseAdjustment;
                }

                // process children
                if (requiresAdjustment)
                {
                    // node was adjusted - need to re-adjust all children less than this node
                    if (Left != null)
                        Left.AdjustIndex(preAdjustmentIndex + Left._relativeIndex, startIndex, adjustment, denseAdjustment, true);
                }
                else
                {
                    // node was not adjusted - need to check children after this node to see if they should be
                    if (Right != null)
                        Right.AdjustIndex(preAdjustmentIndex + Right._relativeIndex, startIndex, adjustment, denseAdjustment, false);
                }
            }

            public Side ChildSide
            {
                get
                {
                    if (Parent.Left == this)
                        return Side.Left;
                    if (Parent.Right == this)
                        return Side.Right;
                    throw new InvalidOperationException("Node is not a child");
                }
            }

            public ChildType ChildType
            {
                get
                {
                    if (_parent == null)
                        return ChildType.Root;
                    return (ChildType)ChildSide;
                }
            }

            public ParentType ParentType
            {
                get
                {
                    if (_right == null)
                    {
                        Debug.Assert(_left == null);
                        return ParentType.Leaf;
                    }
                    if (_left == null)
                        return ParentType.Right;
                    return ParentType.Both;
                }
            }

            public Node ParentToThis
            {
                set
                {
                    if (_parent != null)
                        _parent[ChildSide] = value;
                }

                get
                {
                    return _parent != null
                        ? _parent[ChildSide]
                        : null;
                }
            }

            public int Index
            {
                get { return Parent.Index() + _relativeIndex; }
            }

            public T Data
            {
                get { return _data; }
                internal set { _data = value; }
            }

            // constuctor for regular nodes (that all start life as leaf nodes)
            internal Node(int index, int denseIndex, T data)
            {
                _level = 1;
                _relativeIndex = index;
                _relativeDenseIndex = denseIndex;
                _data = data;
            }

            public override string ToString()
            {
                return string.Format("[{0}] #{1} D[{2}]", _data, Index, DenseIndex);
            }

            public string Html
            {
                get
                {
                    var data = _data.ToString();
                    return string.Format(
                        "<table border><tr><td colspan='2' style='text-align:center; vertical-align:top'>{0}</td></tr>" +
                        "<tr><td style='text-align:center; vertical-align:top'>{1}</td><td style='text-align:center; vertical-align:top'>{2}</td></tr></table>",
                        string.Format("[{0}]<br/>#{1} D{2}<br/>Level {3}", data.Length > 30 ? "..." : data, Index, DenseIndex, _level),
                        Left == null ? "" : Left.Html,
                        Right == null ? "" : Right.Html);
                }
            }

            public void Check()
            {
                if (Left != null && Right == null)
                    throw new InvalidOperationException("Node should not have only a left child");

                if (ParentType == ParentType.Leaf)
                {
                    if (_level != 1)
                        throw new InvalidOperationException("Breaks invariant #1: The level of a leaf node is one");
                }

                if (Left.Level() >= _level)
                    throw new InvalidOperationException("Breaks invariant #2: The level of a left child is strictly less than that of its parent");

                if (Right.Level() > _level)
                    throw new InvalidOperationException("Breaks invariant #3: The level of a right child is less than or equal to that of its parent");

                if (Right.Right().Level() >= _level)
                    throw new InvalidOperationException("Breaks invariant #4: The level of a right grandchild is strictly less than that of its grandparent");

                if (_level > 1)
                {
                    if (ParentType != ParentType.Both)
                        throw new InvalidOperationException("Breaks invariant #5: Every node of level greater than one must have two children");
                }
            }
        }
    }

    public class AnonymousIndexNode<T> : IIndexNode<T>
    {
        public int DenseIndex { get; set; }
        public int Index { get; set; }
        public T Data { get; set; }
        public IIndexNode<T> Next { get; set; }
        public IIndexNode<T> Previous { get; set; }
    }

    public static partial class Extensions
    {
        public static int Level<T>(this VirtualList<T>.Node node)
        {
            return node == null ? 0 : node._level;
        }

        public static VirtualList<T>.Node Child<T>(this VirtualList<T>.Node node, Side side)
        {
            return node == null ? null : node[side];
        }

        public static VirtualList<T>.Node Left<T>(this VirtualList<T>.Node node)
        {
            return node == null ? null : node.Left;
        }

        public static VirtualList<T>.Node Right<T>(this VirtualList<T>.Node node)
        {
            return node == null ? null : node.Right;
        }

        public static Side Opposite(this Side side)
        {
            return side == Side.Left ? Side.Right : Side.Left;
        }

        public static VirtualList<T>.Node FarChild<T>(this VirtualList<T>.Node node, Side side)
        {
            if (node == null)
                return null;

            while (node[side] != null)
                node = node[side];
            return node;
        }

        public static VirtualList<T>.Node First<T>(this VirtualList<T>.Node node)
        {
            return node.FarChild(Side.Left);
        }

        public static VirtualList<T>.Node Last<T>(this VirtualList<T>.Node node)
        {
            return node.FarChild(Side.Right);
        }

        public static int Index<T>(this VirtualList<T>.Node node)
        {
            return node == null ? 0 : node.Index;
        }

        public static int DenseIndex<T>(this VirtualList<T>.Node node)
        {
            return node == null ? 0 : node.DenseIndex;
        }

        public static IEnumerable<VirtualList<T>.Node> DescendendsAndSelf<T>(this VirtualList<T>.Node root)
        {
            var node = root.First();
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        public static VirtualList<T>.Node FindNearestTo<T>(this VirtualList<T>.Node node, Side side, int index)
        {
            // traverse tree until node is found
            while (node != null)
            {
                if (index == node.Index)
                    // exact match
                    return node;

                if (index < node.Index)
                {
                    // closest match?
                    if (node.Left == null)
                    {
                        return side == Side.Right
                            ? node
                            : node.Previous;
                    }

                    node = node.Left;
                }
                else // (index > node.Index)
                {
                    // closest match?
                    if (node.Right == null)
                    {
                        
                        return side == Side.Left
                            ? node
                            : node.Next;
                    }

                    node = node.Right;
                }
            }

            return null;
        }

        public static VirtualList<T>.Node Find<T>(this VirtualList<T>.Node root, int index)
        {
            // traverse tree until node is found
            while (root != null)
            {
                if (index == root.Index)
                    return root;

                root = index < root.Index ? root.Left : root.Right;
            }

            return null;
        }

        public static VirtualList<T>.Node FindByDenseIndex<T>(this VirtualList<T>.Node root, int denseIndex)
        {
            // traverse tree until node is found
            while (root != null)
            {
                if (denseIndex == root.DenseIndex)
                    return root;

                root = denseIndex < root.DenseIndex ? root.Left : root.Right;
            }

            return null;
        }
    }
}
