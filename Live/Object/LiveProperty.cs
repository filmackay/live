using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;


namespace Vertigo.Live
{
    public class LiveProperty<TInner> : LiveValue<TInner>
    {
        public LiveProperty(INotifyPropertyChanged innerObject, string propertyName)
        {
            _innerObject = innerObject;
            _propertyInfo = _innerObject.GetType().GetProperty(propertyName);
            if (_propertyInfo == null)
                throw new InvalidOperationException("Could not find property " + propertyName);

            innerObject.PropertyChanged += InnerPropertyChanged;
            InnerChanged();
        }

        private readonly INotifyPropertyChanged _innerObject;
        private readonly PropertyInfo _propertyInfo;

        private TInner PropertyValue
        {
            get { return (TInner)_propertyInfo.GetValue(_innerObject, null); }
        }

        void InnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyInfo.Name)
                InnerChanged();
        }

        public override void InnerGetValue(ref TInner value, ref StateStatus startType, ref long lastUpdated)
        {
            startType = startType.AddSimple(StateStatus.Connected);
            lastUpdated = HiResTimer.Now();
            value = PropertyValue;
        }

        public override void InnerGetNotify()
        {
        }

        protected override void OnCompleted()
        {
            _innerObject.PropertyChanged -= InnerPropertyChanged;
            base.OnCompleted();
        }
    }

    public static partial class Extensions
    {
        public static LiveProperty<T> LiveProperty<T>(this INotifyPropertyChanged obj, string propertyName)
        {
            return new LiveProperty<T>(obj, propertyName);
        }

        public static object ObjectPropertyPath(this object obj, IEnumerable<string> path)
        {
            if (!path.Any())
                return obj;

            // look for property
            var propertyName = path.First();
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new InvalidOperationException("Could not find property: " + propertyName);
            var propertyValue = propertyInfo.GetValue(obj, null);

            // handle property
            var liveValue = LiveValue.ToUntyped(propertyValue);
            if (liveValue != null)
            {
                // property is live
                return liveValue.PropertyPath(path.Skip(1));
            }

            // property is static
            return propertyValue.ObjectPropertyPath(path.Skip(1));
        }

        public static ILiveValue<object> PropertyPath(this ILiveValue<object> liveProperty, IEnumerable<string> path)
        {
            // flatten the property
            //liveProperty = liveProperty.UnwrapAll();

            // have we arrived at the property?
            if (!path.Any())
                return liveProperty;

            // look for property
            var propertyName = path.First();
            var propertyInfo = liveProperty.Value.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new InvalidOperationException("Could not find property: " + propertyName);

            // handle property
            return liveProperty.SelectStatic(obj => (propertyInfo.GetValue(liveProperty, null)).ObjectPropertyPath(path.Skip(1)));
        }

        public static object ObjectPropertyPath(this object obj, string path)
        {
            return obj.ObjectPropertyPath(path.Split('.'));
        }
    }
}
