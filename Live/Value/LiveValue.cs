using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace Vertigo.Live
{
    public interface ILiveValue<out T> : ILiveObservable<IValueState<T>>
    {
        IValueState<T> DirtySnapshot { get; }
        IValueState<T> Snapshot { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        T Value { get; }
    }

    public abstract partial class LiveValue<T> : ILiveValue<T>
    {
        protected LiveValue()
        {
            _innerChanged = new NotifyLock
            {
                Name = "LiveValue.InnerChanged",
                OnNotify = () =>
                {
                    Subscription[] unnotifiedClients;

                    using (this.Lock())
                    {
                        unnotifiedClients = _notNotifiedClients.ToArray();
                        _notNotifiedClients.Clear();
                    }

                    // notify clients
                    unnotifiedClients.ForEach(o => o.NotifyChange());
                },
            };
        }

        private static readonly ValueState<T> _completedState = new ValueState<T> { Status = StateStatus.Completed };
        private readonly NotifyLock _innerChanged;
        private readonly ValueState<T> _state = new ValueState<T> { Status = StateStatus.Disconnected };
        private readonly HashSet<Subscription> _clients = new HashSet<Subscription>();
        private readonly HashSet<Subscription> _notNotifiedClients = new HashSet<Subscription>();
        protected bool _used;
        private int _direction;
        private readonly WeakReference<LiveValueBinding<T>> _binding = new WeakReference<LiveValueBinding<T>>(null);

        public LiveValueBinding<T> Binding
        {
            get { return _binding.Target ?? (_binding.Target = new LiveValueBinding<T>(this)); }
        }

        public void RunOnRefresh(Action action)
        {
            if (Consumer.Dispatcher == null)
                // no timeline - send directly to dispatcher, dont block as we may be holding a lock that action will need
                action();
            else
                Consumer.Dispatcher.RunOnRefresh(action);
        }

        private IValueState<T> GetState(Subscription client)
        {
            if (client._state.Status == StateStatus.Completed)
                return _completedState;

            using (this.Lock())
            {
                // apply any oustanding changes
                ApplyInnerChange();

                // get state
                var state = client._state.Add(_state);
                if (state.Status == StateStatus.Completing)
                {
                    // complete subscriber
                    _clients.Remove(client);
                }
                else
                {
                    // notify client on next change
                    _notNotifiedClients.Add(client);
                }

                return state;
            }
        }

        public abstract void InnerGetNotify();
        private void GetNotify(Subscription client)
        {
            if (client._state.Status == StateStatus.Completed)
                return;

            using (this.Lock())
            {
                // notify client on next change
                InnerGetNotify();
                _notNotifiedClients.Add(client);
            }
        }

        private bool ConnectClient(Subscription client)
        {
            using (this.Lock())
            {
                return _clients.Add(client);
            }
        }

        private void CompleteClient(Subscription client, bool graceful)
        {
            bool notify;
            using (this.Lock())
            {
                client._state.AddInline(StateStatus.Completing);

                // notify client, if required
                notify = _notNotifiedClients.Remove(client) && graceful;

                // if immediate shutdown, remove it now
                if (!graceful)
                    _clients.Remove(client);
            }

            if (notify)
                client.NotifyChange();
        }

        public IDisposable Subscribe(ILiveObserver<IValueState<T>> observer)
        {
            var subscription = new Subscription(this, observer);
            //Publish.OnConsume(subscription.Start);
            subscription.Start();
            return subscription;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IValueState<T> Snapshot
        {
            get
            {
                ApplyInnerChange();
                return DirtySnapshot;
            }
        }

        public IValueState<T> DirtySnapshot
        {
            get { return _state; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get { return Snapshot.NewValue; }
        }

        public int Direction
        {
            get { return _direction; }
        }

        public abstract void InnerGetValue(ref T value, ref StateStatus startType, ref long lastUpdated);
        protected bool ApplyInnerChange()
        {
            if (!_innerChanged.Status.IsNotified())
                return false;

            // any change to process?
            using (this.Lock())
            {
                if (!_innerChanged.Status.IsNotified())
                    return false;
                _used = true;

                // update inner value (may call InnerChanged())
                var value = _state.NewValue;
                var status = _state.Status;
                var lastUpdated = 0L;
                _innerChanged.Process(notified => InnerGetValue(ref value, ref status, ref lastUpdated));
                _state.NewValue = value;
                _state.Status = status;
                _state.LastUpdated = lastUpdated;

                return true;
            }
        }

        public void InnerChanged()
        {
            _innerChanged.Notify();
        }

        public void InnerUnchanged()
        {
            _innerChanged.Unnotify();
        }

        protected virtual void OnCompleted()
        {
            var binding = _binding.Target;
            if (binding != null)
                binding.Dispose();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        // binding implementation
        private List<RoutedPropertyChangedEventHandler<T>> _valueChangedClients;
        private List<RoutedPropertyChangedEventHandler<int>> _directionChangedClients;
        private LiveObserver<IValueState<T>> _bindingObserver;

        private void CheckBinding()
        {
            if (_bindingObserver != null)
            {
                // subscribed
                if (_valueChangedClients == null && _directionChangedClients == null)
                {
                    _bindingObserver.Dispose();
                    _bindingObserver = null;
                }
            }
            else
            {
                // not subscribed
                if (_valueChangedClients != null || _directionChangedClients != null)
                {
                    Subscribe(_bindingObserver = this.CreateObserver(() => RunOnRefresh(UpdateBinding)));
                    _direction = 0;
                }
            }
        }

        public event RoutedPropertyChangedEventHandler<T> ValueChanged
        {
            add
            {
                if (_valueChangedClients == null)
                {
                    // start binding
                    _valueChangedClients = new List<RoutedPropertyChangedEventHandler<T>>();
                    CheckBinding();
                }
                _valueChangedClients.Add(value);
            }
            remove
            {
                if (_valueChangedClients.Count == 1)
                {
                    // shutdown binding
                    _valueChangedClients = null;
                    CheckBinding();
                }
                else
                    _valueChangedClients.Remove(value);
            }
        }

        public event RoutedPropertyChangedEventHandler<int> DirectionChanged
        {
            add
            {
                if (_directionChangedClients == null)
                {
                    // start binding
                    _directionChangedClients = new List<RoutedPropertyChangedEventHandler<int>>();
                    CheckBinding();
                }
                _directionChangedClients.Add(value);
            }
            remove
            {
                if (_directionChangedClients.Count == 1)
                {
                    // shutdown binding
                    _directionChangedClients = null;
                    CheckBinding();
                }
                else
                    _directionChangedClients.Remove(value);
            }
        }

        private void UpdateDirectionBinding(int newDirection)
        {
            var oldDirection = _direction;
            var arg = new RoutedPropertyChangedEventArgs<int>(oldDirection, newDirection);
            _directionChangedClients
                .ForEach(directionChangedClient => directionChangedClient(this, arg));
            _direction = newDirection;
        }

        private void UpdateValueBinding(T oldValue, T newValue)
        {
            var arg = new RoutedPropertyChangedEventArgs<T>(oldValue, newValue);
            _valueChangedClients
                .ForEach(directionChangedClient => directionChangedClient(this, arg));
        }

        private void UpdateBinding()
        {
            // change has occurred
            var state = _bindingObserver.GetState();
            var oldDirection = _direction;
            var newDirection = _direction;
            if (state.Status == StateStatus.Connected)
            {
                if (!state.HasChange)
                    return;

                if (state.NewValue is IComparable<T>)
                {
                    var compare = Comparer<T>.Default.Compare(state.NewValue, state.OldValue);
                    if (compare != 0)
                        newDirection = compare;
                }
            }
            if (state.Status.IsConnected())
            {
                // work out what needs to be set
                var reiterateDirection = (oldDirection == newDirection && oldDirection != 0);
                var setDirection = reiterateDirection || oldDirection != newDirection;

                // send update
                Consumer.Dispatcher.RunOnDispatcher(() =>
                {
                    if (reiterateDirection)
                        UpdateDirectionBinding(0);
                    UpdateValueBinding(state.OldValue, state.NewValue);
                    if (setDirection)
                        UpdateDirectionBinding(newDirection);
                });
            }
            if (state.Status == StateStatus.Completing)
                Consumer.Dispatcher.RunOnDispatcher(() => UpdateValueBinding(state.OldValue, default(T)));
        }
    }

    public static partial class LiveValue
    {
        public static IObservable<string> Trace<T>(this ILiveValue<T> source, string tag)
        {
            return source.Trace(tag.ToLive());
        }

        public static IObservable<string> Trace<T>(this ILiveValue<T> source, ILiveValue<string> tag)
        {
            return source
                .Tag(tag)
                .Values();
        }

        public static ILiveValue<string> Tag<T>(this ILiveValue<T> source, ILiveValue<string> tag)
        {
            return
                new Func<IValueState<T>, IValueState<string>, string>(
                    (itemState, tagState) => string.Format("{0} {1} ({2:F3}ms)", tagState.NewValue, itemState, itemState.Latency().TotalMilliseconds))
                    .LiveInvoke(source, tag);
        }
    }
}