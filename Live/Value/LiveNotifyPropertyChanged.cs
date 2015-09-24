using System;
using System.ComponentModel;

namespace Vertigo.Live
{
    public class LiveNotifyPropertyChanged<TInner> : INotifyPropertyChanged, IDisposable
    {
        private static readonly PropertyChangedEventArgs _propertyChangedEventArgs = new PropertyChangedEventArgs("Value");
        private readonly LiveObserver<IValueState<TInner>> _observer;
        public long TotalNotifications { get; private set; }

        public LiveNotifyPropertyChanged(ILiveValue<TInner> source)
        {
            source.Subscribe(
                _observer = source.CreateObserver(
                    () =>
                    {
                        using (this.Lock())
                        {
                            if (ValueChanged != null)
                                ValueChanged(Value);
                            if (_propertyChanged != null)
                                _propertyChanged(this, _propertyChangedEventArgs);
                            TotalNotifications++;
                        }
                    }));
        }

        public TInner Value
        {
            get { return _observer.Last.NewValue; }
        }

        private event PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                using (this.Lock())
                {
                    _propertyChanged += value;
                    value(this, _propertyChangedEventArgs);
                }
            }

            remove
            {
                using (this.Lock())
                {
                    _propertyChanged -= value;
                }
            }
        }

        public event Action<TInner> ValueChanged;

        public void Dispose()
        {
            _observer.Dispose();
        }
    }

    public partial class Extensions
    {
        public static LiveNotifyPropertyChanged<TInner> ToNotify<TInner>(this ILiveValue<TInner> inner)
        {
            return new LiveNotifyPropertyChanged<TInner>(inner);
        }
    }
}