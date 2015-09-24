using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveListBinding : LiveDependencyObject
    {
        class InnerList : IList<object>, INotifyCollectionChanged
        {
            private readonly LiveListBinding _parent;

            public InnerList(LiveListBinding parent)
            {
                _parent = parent;
            }

            public NotifyCollectionChangedEventHandler _collectionChanged;
            public event NotifyCollectionChangedEventHandler CollectionChanged
            {
                add
                {
                    // we should only have one event subscriber
                    Debug.Assert(_collectionChanged == null);
                    _collectionChanged = value;
                }
                remove
                {
                    // we should unsubscribe now
                    Debug.Assert(_collectionChanged == value);
                    _collectionChanged = null;
                    if (_parent._subscription != null)
                    {
                        _parent._subscription.Dispose();
                        _parent._subscription = null;
                    }
                }
            }

            public List<object> Inner = new List<object>();

#region IList<object> implementation

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                return Inner.GetEnumerator();
            }

            public IEnumerator GetEnumerator()
            {
                return Inner.GetEnumerator();
            }

            public void CopyTo(object[] array, int index)
            {
                Inner.CopyTo(array, index);
            }

            public int Count
            {
                get { return Inner.Count; }
            }

            public bool IsReadOnly
            {
                get { return ((IList<object>)Inner).IsReadOnly; }
            }

            public void Add(object value)
            {
                Inner.Add(value);
            }

            public bool Contains(object value)
            {
                return Inner.Contains(value);
            }

            public void Clear()
            {
                Inner.Clear();
            }

            public int IndexOf(object value)
            {
                return Inner.IndexOf(value);
            }

            public void Insert(int index, object value)
            {
                Inner.Insert(index, value);
            }

            public bool Remove(object value)
            {
                return Inner.Remove(value);
            }

            public void RemoveAt(int index)
            {
                Inner.RemoveAt(index);
            }

            public object this[int index]
            {
                get { return Inner[index]; }
                set { Inner[index] = value; }
            }
#endregion
        }

        public static readonly DependencyProperty ListProperty = DependencyProperty.Register("List", typeof(IList<object>), typeof(LiveListBinding));
        private readonly InnerList _list;
        private IDisposable _subscription;
        private readonly LiveObserver<ICollectionState<object, IListDelta<object>>> _observer;
        public ILiveList<object> Source { get; private set; }

        public LiveListBinding(ILiveList<object> source)
        {
            Source = source;
            _list = new InnerList(this);
            SetValue(ListProperty, _list);

            // subscribe
            _subscription = Source.Subscribe(_observer = Source.CreateObserver(() => RunOnRefresh(UpdateSubscription)));
        }

        private void UpdateSubscription()
        {
            // change has occurred
            using (var state = _observer.GetState())
            {
                if (state.Status.IsConnecting())
                {
                    // get snapshot
                    var list = state
                        .Inner
                        .ToList();
                    _list.Inner = list;

                    // notify
                    var collectionChanged = _list._collectionChanged;
                    if (collectionChanged != null)
                        RunOnDispatcher(() => collectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
                }
                else if (state.Delta != null)
                {
                    var delta = state.Delta;

                    var collectionChanged = _list._collectionChanged;
                    if (collectionChanged != null)
                        RunOnDispatcher(() =>
                            {
                                // _list dosen't get used moving forward so don't bother updating it
                                //delta
                                //    .ToMutable<object, IListDelta<object>, IList<object>>()
                                //    .ApplyTo(_list.Inner);

                                try
                                {
                                    foreach (var indexDelta in delta.IndexDeltas)
                                    {
                                        var deleteItems = indexDelta.Data.DeleteItems as IList<object>;
                                        var insertItems = indexDelta.Data.InsertItems as IList<object>;
                                        var replaceCount = Math.Min(deleteItems.Count, insertItems.Count);
                                        var insertCount = insertItems.Count - replaceCount;
                                        var deleteCount = deleteItems.Count - replaceCount;

                                        for (var i = 0; i < replaceCount; i++)
                                        {
                                            collectionChanged(this,
                                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                    insertItems[i],
                                                    deleteItems[i],
                                                    indexDelta.Index + i));
                                        }

                                        for (var i = 0; i < insertCount; i++)
                                        {
                                            collectionChanged(this,
                                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                    insertItems[replaceCount + i],
                                                    indexDelta.Index + replaceCount + i));
                                        }

                                        for (var i = 0; i < deleteCount; i++)
                                        {
                                            collectionChanged(this,
                                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                    deleteItems[replaceCount + i],
                                                    indexDelta.Index + replaceCount));
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogManager.Entry(LogType.Error, "LiveListBinding", e.ToString());
                                }
                            });
                }
            }
        }
    }
}