using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;

namespace Vertigo.Live
{
    public class LiveValueDependencyObject : LiveDependencyObject
    {
        private ISubscription _subscription;
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(LiveValueDependencyObject));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(int), typeof(LiveValueDependencyObject));

        private ILiveValue _source;
        public ILiveValue Source
        {
            get { return _source; }
            set
            {
                _source = value;
                SetupSubscription();
            }
        }

        private int _direction;

        private void SetupSubscription()
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
            var state = (IValueState)_subscription.GetState();
            var oldDirection = _direction;
            if (state.Status == StateStatus.Started)
            {
                if (!state.HasDelta)
                    return;

                if (state.NewValue is IComparable)
                {
                    var compare = Comparer.Default.Compare(state.NewValue, state.OldValue);
                    if (compare != 0)
                        _direction = compare;
                }
            }
            if (state.Status.IsStarted())
            {
                // send update
                RunOnDispatcher(_direction == oldDirection && oldDirection != 0 ?
                    new Action(() =>
                    {
                        SetValue(DirectionProperty, 0);
                        SetValue(ValueProperty, state.NewValue);
                        SetValue(DirectionProperty, _direction);
                    }) :
                    () =>
                    {
                        SetValue(ValueProperty, state.NewValue);
                        SetValue(DirectionProperty, _direction);
                    });
            }

            if (state.Status == StateStatus.Stopping)
            {
                RunOnDispatcher(() => ClearValue(ValueProperty));
            }
        }
    }
}