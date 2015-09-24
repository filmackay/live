using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Vertigo.Live
{
    public abstract class LiveValuePublisher<T> : LiveValue<T>
    {
        protected LiveValuePublisher()
        {
            _notifyUncommittedChange = new NotifyLock
            {
                OnNotify = () => Publish.OnPublish(Commit),
                Name = "LiveValue.UncommitedChange",
            };
        }

        private readonly ValueState<T> _uncommitted = new ValueState<T> { Status = StateStatus.Disconnected };
        private readonly ValueState<T> _committed = new ValueState<T> { Status = StateStatus.Disconnected };
        private readonly NotifyLock _notifyUncommittedChange;

        protected void Init(T newValue)
        {
            Init(newValue, HiResTimer.Now());
        }

        protected void Init(T newValue, long lastUpdated)
        {
            using (this.Lock())
            {
                if (!_used)
                {
                    // bypass commit cycle for initial value
                    _uncommitted.Status = StateStatus.Connected;
                    _committed.Status = StateStatus.Connecting;
                    _committed.NewValue = _uncommitted.NewValue = newValue;
                    _committed.LastUpdated = lastUpdated;
                    InnerChanged();
                    return;
                }
            }

            // we have already started - fall back to normal Start
            Connect(newValue, lastUpdated);
        }

        private bool Set(IValueState<T> state)
        {
            using (Publish.Transaction())
            {
                bool changed;
                using (this.Lock())
                    changed = _uncommitted.AddInline(state);
                if (changed)
                    _notifyUncommittedChange.Notify();
                return changed;
            }
        }

        private bool Set(StateStatus status, T value, long lastUpdated)
        {
            return Set(new ValueState<T> { NewValue = value, Status = status, LastUpdated = lastUpdated });
        }

        private bool Set(StateStatus status)
        {
            return Set(new ValueState<T> { NewValue = _uncommitted.NewValue, Status = status, LastUpdated = HiResTimer.Now() });
        }

        protected bool SetValue(T newValue)
        {
            // optimisation: if value does not get changed
            if (_uncommitted.Status.IsConnected() && FussyEqualityComparer<T>.Equals(_uncommitted.NewValue, newValue))
                return false;
            return Set(StateStatus.Connected, newValue, HiResTimer.Now());
        }

        protected bool SetValue(T newValue, long timestamp)
        {
            return Set(StateStatus.Connected, newValue, timestamp);
        }

        protected T GetValue()
        {
            return _uncommitted.NewValue;
        }

        protected void Connect(T newValue)
        {
            Connect(newValue, HiResTimer.Now());
        }

        protected void Connect(T newValue, long lastUpdated)
        {
            Set(StateStatus.Connecting, newValue, lastUpdated);
        }

        protected void Disconnect()
        {
            Set(StateStatus.Disconnected);
        }

        protected void Complete()
        {
            Set(StateStatus.Completed);
            base.OnCompleted();
        }

        private void Commit()
        {
            var notify = false;
            _notifyUncommittedChange.Process(notified => notify = _committed.AddInline(_uncommitted));
            if (notify)
                InnerChanged();
        }

        public override sealed void InnerGetValue(ref T value, ref StateStatus status, ref long lastUpdated)
        {
            using (this.Lock())
            {
                var state = _committed.Extract(true);
                value = state.NewValue;
                status = state.Status;
                lastUpdated = state.LastUpdated;
            }
        }

        public override void InnerGetNotify()
        {
            InnerUnchanged();
        }
    }
}