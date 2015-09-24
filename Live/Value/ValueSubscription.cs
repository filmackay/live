using System;
using System.Diagnostics;

namespace Vertigo.Live
{
    public partial class LiveValue<T>
    {
        private sealed class Subscription : Subscription<LiveValue<T>, IValueState<T>, ValueState<T>>
        {
            public Subscription(LiveValue<T> source, ILiveObserver<IValueState<T>> observer)
                : base(source, null, observer)
            {
            }

            protected override bool _connect()
            {
                return Source.ConnectClient(this);
            }

            protected override void _complete(bool graceful)
            {
                Source.CompleteClient(this, graceful);
            }

            protected override IValueState<T> getState(IDisposable stateLock)
            {
                // value states do not need locks
                Debug.Assert(stateLock == null);

                // if we are not notified, return old state
                if (_notify.Status == NotifyStatus.None)
                {
                    _state.NextInline();
                    return _state;
                }

                // get the state and return a copy
                return Source.GetState(this);
            }

            protected override void getNotify()
            {
                Source.GetNotify(this);
            }
        }
    }
}