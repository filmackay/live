using System;
using System.Collections;
using System.Windows;

namespace Vertigo.Live
{
    public class LiveValueBinding<T> : LiveDependencyObject, IDisposable
    {
        private readonly ILiveValue<T> _source;
        private readonly Live<T> _liveSource;
        private readonly LiveValueWriter<T> _liveWriter;
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(T), typeof(LiveValueBinding<T>), new PropertyMetadata((d, e) => (d as LiveValueBinding<T>).ValueChangedCallback(e)));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(int), typeof(LiveValueBinding<T>));
        private int _direction;
        private bool _settingValue;
        private readonly LiveObserver<IValueState<T>> _observer;

        public LiveValueBinding(ILiveValue<T> source)
        {
            _source = source;
            _liveSource = source as Live<T>;
            _liveWriter = source as LiveValueWriter<T>;

            // subscribe
            _source.Subscribe(_observer = _source.CreateObserver(() => RunOnRefresh(UpdateSubscription)));
        }

        private void ValueChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            // if we are directly setting a value, or committed values are propogating into WPF ignore the change notification
            if (_settingValue) // || Publish.Status == TransactionStatus.Commit)
                return;

            if (_liveSource == null && _liveWriter == null)
                throw new InvalidOperationException("Live value is read-only");

            // perform change on threadpool ??
            if (_liveSource != null)
                _liveSource.PublishValue = (T)e.NewValue;
            else
            {
                _liveWriter.PublishValue = (T)e.NewValue;
                LiveValueWriters.CheckForUpdates();
            }
        }

        private void UpdateSubscription()
        {
            // change has occurred
            var state = _observer.GetState();
            var oldDirection = _direction;
            if (state.Status.IsDeltaRelevant())
            {
                if (!state.HasChange)
                    return;

                if (state.NewValue is IComparable)
                {
                    var compare = Comparer.Default.Compare(state.NewValue, state.OldValue);
                    if (compare != 0)
                        _direction = compare;
                }
            }
            if (state.Status.IsConnected())
            {
                // work out what needs to be set
                var resetDirection = (_direction == oldDirection && oldDirection != 0);
                var setDirection = resetDirection || _direction != oldDirection;

                // send update
                RunOnDispatcher(() =>
                    {
                        if (resetDirection)
                            SetValue(DirectionProperty, 0);
                        _settingValue = true;
                        SetValue(ValueProperty, state.NewValue);
                        _settingValue = false;
                        if (setDirection)
                            SetValue(DirectionProperty, _direction);
                    });
            }

            if (state.Status == StateStatus.Completing)
            {
                RunOnDispatcher(() => ClearValue(ValueProperty));
            }
        }

        public void Dispose()
        {
            _observer.Dispose();
        }
    }
}