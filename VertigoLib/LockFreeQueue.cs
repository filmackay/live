using System;
using System.Collections.Generic;
using System.Threading;

namespace Vertigo
{
    public class LockFreeQueue<T>
    {
        class Node
        {
            private readonly Node _next;
            private readonly T _data;

            public Node(Node next, T data)
            {
                _next = next;
                _data = data;
            }

            public T Data { get { return _data; } }
            public Node Next { get { return _next; } }
        }

        private Node _head;

        public bool IsEmpty
        {
            get { return _head == null; }
        }

        public void Enqueue(T item)
        {
            while (true)
            {
                // try to add to head
                var oldHead = _head;
                if (Interlocked.CompareExchange(ref _head, new Node(oldHead, item), oldHead) == oldHead)
                    break;
            }
        }

        public List<T> GetAll()
        {
            while (true)
            {
                var node = _head;
                if (Interlocked.CompareExchange(ref _head, null, node) == node)
                {
                    // put all items into array
                    var ret = new List<T>();
                    while (node != null)
                    {
                        ret.Insert(0, node.Data);
                        node = node.Next;
                    }
                    return ret;
                }
            }
        }

        public IEnumerable<T> Peek()
        {
            var node = _head;
            while (node != null)
            {
                yield return node.Data;
                node = node.Next;
            }
        }
    }
}