using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Vertigo.Live
{
    public class LivePropertiesSubscription<T> : IDisposable
        where T : class, ILiveEntity<T>, new()
    {
        public class Info
        {
            public ILive Observable;
            public ISubscription Observer;
            public PropertyInfo PropertyInfo;
        }

        private readonly T _entity;
        private readonly Action<LivePropertiesSubscription<T>> _notifyChange;
        private readonly List<Info> _properties = new List<Info>();
        private readonly List<Info> _propertyChanges = new List<Info>();
        private readonly NotifyLock _innerChanged;

        public LivePropertiesSubscription(T entity, Action<LivePropertiesSubscription<T>> setObserver, Action<LivePropertiesSubscription<T>> notifyChange)
        {
            _entity = entity;
            _notifyChange = notifyChange;
            setObserver(this);

            // setup change notifier
            _innerChanged = new NotifyLock { OnNotify = () => _notifyChange(this) };

            // observe all live properties
            foreach (var propertyInfo in typeof(T).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Live<>)))
            {
                var info = new Info
                {
                    PropertyInfo = propertyInfo,
                    Observable = propertyInfo.GetValue(_entity, null) as ILive,
                };

                info.Observable.Subscribe(observer =>
                {
                    info.Observer = observer;
                    _properties.Add(info);
                },
                observer =>
                {
                    lock (this)
                    {
                        _propertyChanges.Add(info);
                    }
                    InnerChanged();
                });
            }
        }

        protected void InnerChanged()
        {
            _innerChanged.Notify();
        }

        public void Dispose()
        {
            foreach (var observer in _properties)
                observer.Observer.Dispose();
        }

        public PropertyInfo[] GetChanges()
        {
            lock(this)
            {
                var ret = _propertyChanges.Select(info =>
                {
                    info.Observer.GetState();
                    return info.PropertyInfo;
                }).ToArray();
                _propertyChanges.Clear();
                return ret;
            }
        }

        public T Observable
        {
            get { return _entity; }
        }

        public Timeline Timeline { get; set;}
    }
}
