using System;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Vertigo.Live
{
    public class LivePropertiesSubscription : IDisposable
    {
        private readonly object _target;
        private readonly PropertyInfo[] _propertyInfos;
        private readonly List<Tuple<ILiveValue<object>, LiveObserver<IValueState<object>>, PropertyInfo>> _subscriptions = new List<Tuple<ILiveValue<object>, LiveObserver<IValueState<object>>, PropertyInfo>>();
        private readonly NotifySet<Tuple<ILiveValue<object>, LiveObserver<IValueState<object>>, PropertyInfo>> _changes = new NotifySet<Tuple<ILiveValue<object>, LiveObserver<IValueState<object>>, PropertyInfo>>();

        public LivePropertiesSubscription(object target, IEnumerable<PropertyInfo> properties)
        {
            _propertyInfos = properties.ToArray();
            _target = target;
        }

        public object Target
        { get { return _target; } }

        public void Connect(Action onNotify)
        {
            _changes.OnNotify = onNotify;

            // observe all live properties
            foreach (var propertyInfo in _propertyInfos)
            {
                var propertyValue = propertyInfo.GetValue(_target, null);
                var target = LiveValue.ToUntyped(propertyValue);
                if (target != null)
                {
                    // subscribe to target
                    var info = default(Tuple<ILiveValue<object>, LiveObserver<IValueState<object>>, PropertyInfo>);
                    var observer = target.CreateObserver(o => Publish.OnConsume(() =>
                        {
                            var state = o.GetState();
                            if (state.Status.IsDeltaRelevant() && state.HasChange)
                                _changes.Add(info);
                            if (state.Status == StateStatus.Completing)
                            {
                                _subscriptions.Remove(info);
                                if (_subscriptions.Count == 0)
                                    Dispose();
                            }
                        }));
                    info = Tuple.Create(target, observer, propertyInfo);
                    target.Subscribe(observer);
                    observer.GetState();
                }
            }
        }

        public void Dispose()
        {
            using (this.Lock())
            {
                foreach (var info in _subscriptions.ToArray())
                    info.Item2.Dispose();
            }
        }

        public void ClearChanges()
        {
            _changes.Get();
        }

        public PropertyInfo[] GetChanges()
        {
            return _changes.Get()
                .Select(i => i.Item3)
                .ToArray();
        }
    }

    public static partial class Extensions
    {
        public static LivePropertiesSubscription SubscribeToLiveProperties<T>(this T entity)
        {
            return new LivePropertiesSubscription(entity,
                typeof(T)
                    .GetProperties()
                    .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Live<>)));
        }

        public static LivePropertiesSubscription SubscribeToColumns<T>(this T entity)
        {
            return new LivePropertiesSubscription(entity,
                typeof(T)
                    .GetProperties()
                    .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Live<>) && p.CustomAttribute<ColumnAttribute>() != null));
        }
    }
}
