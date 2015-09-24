using System;
using System.Linq;
using System.Collections.Generic;

namespace Vertigo.Live
{
    /*
    public class LiveMultiObserver
    {
        public enum ReferenceType
        {
            Property,
            CollectionItem,
        }

        public class Reference
        {
            public ReferenceType ReferenceType;
            public object Parent;
        }

        public class LiveInfo : IDisposable
        {
            public int ID;
            public ISubscription<object> Subscriber;
            public ILiveValue<object> Live;
            public List<Reference> References = new List<Reference>();

            public void Dispose()
            {
                Live.Dispose();
            }
        }

        private readonly ILive _root;
        private readonly Dictionary<int, LiveInfo> _infoByID = new Dictionary<int, LiveInfo>();
        private readonly Dictionary<ILive, LiveInfo> _infoByObservable = new Dictionary<ILive, LiveInfo>();
        private readonly HashSet<LiveInfo> _changed = new HashSet<LiveInfo>();
        private int _maxID;

        public LiveMultiObserver(ILive root)
        {
            _root = root;
            Observe(root);
        }

        private void Observe(object obj)
        {
            lock (this)
            {
                // live values
                var liveValue = obj as ILiveValue;
                if (liveValue != null)
                {
                    // already observing?
                    LiveInfo observerInfo;
                    if (_infoByObservable.TryGetValue(liveValue, out observerInfo))
                    {
                        // already observing
                        observerInfo.References.Add(new Reference
                        {
                            Parent = liveValue,
                            ReferenceType = ReferenceType.Property,
                        });
                        return;
                    }

                    // need to start observing
                    observerInfo = new LiveInfo
                    {
                        ID = ++_maxID,
                        Live = liveValue,
                        References =
                        {
                            new Reference
                            {
                                Parent = liveValue,
                                ReferenceType = ReferenceType.Property,
                            }
                        },
                    };
                    observerInfo.Subscriber = liveValue.Subscribe();
                    lock (this)
                        _changed.Add(observerInfo);
                    observerInfo.Subscriber.Connect(() =>
                        {
                            _infoByID.Add(observerInfo.ID, observerInfo);
                            _infoByObservable.Add(observerInfo.Live, observerInfo);

                            var state = observerInfo.Subscriber.GetState();
                            if (state.Status == StateStatus.Completing)
                                lock (this)
                                    _changed.Remove(observerInfo);
                        });
                    return;
                }

                var liveCollection = obj as ILiveCollection;
                if (liveCollection != null)
                {
                    return;
                }

                // check out observable properties
                foreach (var propInfo in obj.GetType().GetProperties().Where(p => p.PropertyType.IsSubclassOfGeneric(typeof(ILive))))
                {
                    var propValue = propInfo.GetValue(obj, null);
                    Observe(propValue);
                }
            }
        }

        private void Unobserve(ILive live)
        {
            lock (this)
            {
                LiveInfo observerInfo;
                if (!_infoByObservable.TryGetValue(live, out observerInfo))
                    return;

                // release reference
                //if (--observerInfo.ReferenceCount == 0)
                //{
                //    _infoByID.Remove(observerInfo.ID);
                //    _infoByObservable.Remove(observerInfo.Observable);
                //    observerInfo.Dispose();
                //}
            }
        }

        public void GetChanges()
        {
            var ret = new List<Tuple<LiveInfo, IState>>();
            lock (this)
            {
                while (_changed.Any())
                {
                    var changes = _changed.ToArray();
                    _changed.Clear();

                    foreach (var observerInfo in changes)
                    {
                        // add change
                        var change = observerInfo.Subscriber.GetState();
                        ret.Add(new Tuple<LiveInfo, IState>(observerInfo, change));

                        var liveValue = observerInfo.Subscriber as IValueSubscription;
                        if (liveValue != null)
                        {
                            // live result?
                            var valueChange = change as IValueState;
                            if (liveValue.Source.InnerType is ILiveValue)
                            {
                                switch (change.Status)
                                {
                                    case StateStatus.Connecting:
                                        Observe(valueChange.NewValue as ILiveValue);
                                        break;
                                    case StateStatus.Connected:
                                        if (change.HasDelta)
                                        {
                                            Observe(valueChange.NewValue as ILiveValue);
                                            Unobserve(valueChange.OldValue as ILiveValue);
                                        }
                                        break;
                                    case StateStatus.Completing:
                                        Unobserve(valueChange.OldValue as ILiveValue);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static partial class Extensions
    {
        public static LiveMultiObserver LiveObserver(this ILive root)
        {
            return new LiveMultiObserver(root);
        }
    }*/
}
