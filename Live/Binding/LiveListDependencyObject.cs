using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Vertigo.Live
{
    public class LiveListDependencyObject : LiveDependencyObject
    {
        class ListEnumerable : IEnumerable, INotifyCollectionChanged
        {
            private readonly LiveListDependencyObject _parent;

            public ListEnumerable(LiveListDependencyObject parent)
            {
                _parent = parent;
            }

            public IEnumerator GetEnumerator()
            {
                if (_startInner == null)
                    return Enumerable
                        .Empty<object>()
                        .GetEnumerator();

                var ret = _startInner.GetEnumerator();
                //_startInner = null;
                return ret;
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

            public IList _startInner;
        }

        public static readonly DependencyProperty ListProperty = DependencyProperty.Register("List", typeof(IEnumerable), typeof(LiveListDependencyObject));
        private readonly ListEnumerable _listProperty;
        private ILiveList _source;
        private IList _mirror;
        private ISubscription _subscription;
        public ILiveList Source
        {
            get { return _source; }
            set
            {
                _source = value;
                SetupObserver();
            }
        }

        public LiveListDependencyObject()
        {
            _listProperty = new ListEnumerable(this);

            SetValue(ListProperty, _listProperty);
        }

        private void SetupObserver()
        {
            _subscription.SafeDispose();
            if (_source == null)
                return;

            _subscription = _source.Subscribe();
            if (_subscription.Start(() => DispatcherConsumer.Global.RunOnRefresh(UpdateSubscription), null))
                UpdateSubscription();
        }

        private void UpdateSubscription()
        {
            // change has occurred
            using (var state = _subscription.GetState() as ICollectionState)
            {
                if (state.Status == StateStatus.Starting)
                {
                    var list = state
                        .Inner
                        .Cast<object>()
                        .ToList();
                    _listProperty._startInner = list;
                    _mirror = list.ToList();
                    NotifyCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else if (state.Delta != null)
                {
                    var delta = state.Delta as IListDelta;

                    delta.ApplyTo(_mirror);
                    delta.ApplyTo(_listProperty._startInner);

                    foreach (var indexDelta in delta.IndexDeltas)
                    {
                        var deleteItems = indexDelta.Data.DeleteItems as IList;
                        var insertItems = indexDelta.Data.InsertItems as IList;
                        var replaceCount = Math.Min(deleteItems.Count, insertItems.Count);
                        var insertCount = insertItems.Count - replaceCount;
                        var deleteCount = deleteItems.Count - replaceCount;

                        if (replaceCount > 0)
                            NotifyCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                                     insertItems.Cast<object>().Take(replaceCount).ToArray(),
                                                                     deleteItems.Cast<object>().Take(replaceCount).ToArray(),
                                                                     indexDelta.Index));

                        if (insertCount > 0)
                            NotifyCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                                     insertItems.Cast<object>().Skip(replaceCount).Take(insertCount).ToArray(),
                                                                     indexDelta.Index + replaceCount));
                        if (deleteCount > 0)
                            NotifyCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                     deleteItems.Cast<object>().Skip(replaceCount).Take(deleteCount).ToArray(),
                                                                     indexDelta.Index + replaceCount));
                    }
                }
            }
        }

        public void NotifyCollectionChangedEvent(NotifyCollectionChangedEventArgs e)
        {
            var collectionChanged = _listProperty._collectionChanged;
            if (collectionChanged != null)
            {
                //Debug.Print("Queue: {0} #{1} {2} / #{3} {4}",
                //    e.Action,
                //    e.NewStartingIndex,
                //    e.NewItems == null ? "" : string.Join(",", e.NewItems.OfType<object>().Select(i => i.ToString()).ToArray()),
                //    e.OldStartingIndex,
                //    e.OldItems == null ? "" : string.Join(",", e.OldItems.OfType<object>().Select(i => i.ToString()).ToArray()));
                RunOnDispatcher(() => collectionChanged(this, e));
            }
        }
    }
}