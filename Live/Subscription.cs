using System;
using System.Diagnostics;

namespace Vertigo.Live
{
    public interface ILiveSubscription<out TIState> : IDisposable
        where TIState : IState
    {
        IDisposable GetStateLock(bool block = true);
        TIState GetState(IDisposable stateLock);
        void GetNotify();
        void Terminate();
    }

    public abstract class Subscription<TILiveObservable, TIState, TState> : ILiveSubscription<TIState>
        where TILiveObservable : ILiveObservable<TIState>
        where TIState : class, IState
        where TState : State<TIState, TState>, TIState, new()
    {
        private readonly Locker _getStateLock;
        protected readonly ILiveObserver<TIState> _observer;
        protected readonly NotifyLock _notify;
        protected TILiveObservable Source;
        internal TState _state = new TState {Status = StateStatus.Disconnected};

        protected Subscription(TILiveObservable source, Locker getStateLock, ILiveObserver<TIState> observer)
        {
            _getStateLock = getStateLock;
            _observer = observer;
            Source = source;
            _notify = new NotifyLock {OnNotify = _observer.OnNotify};
            observer.Bind(this);
        }

        public void Start()
        {
            using (Source.Lock())
            {
                if (_connect())
                    _notify.Notify();
            }
        }

        public IDisposable GetStateLock(bool block = true)
        {
            return _getStateLock == null ? null : _getStateLock(block);
        }

        protected abstract bool _connect();
        protected abstract void _complete(bool graceful);

        public void Dispose()
        {
            // graceful completion
            _complete(true);
        }

        public void Terminate()
        {
            // ungraceful completion
            _complete(false);
        }

        protected abstract TIState getState(IDisposable stateLock);
        public TIState GetState(IDisposable stateLock)
        {
            using (Source.Lock())
            {
                // get latest state
                _notify.Process(notified =>
                {
                    var newState = getState(stateLock);
                    _state.AddInline(newState);
                });

                // get client state
                var clientState = _state.Extract(true);
                if (clientState.Status == StateStatus.Completing)
                     Dispose();

                return clientState;
            }
        }

        protected abstract void getNotify();
        public void GetNotify()
        {
            using (Source.Lock())
            {
                // get latest state
                _notify.Process(notified => getNotify());
            }
        }

        public void NotifyChange()
        {
            _notify.Notify();
        }

        public bool HasChanged
        {
            get { return _notify.Status.IsNotified(); }
        }
    }

    public static partial class Extensions
    {
        public static TIState GetState<TIState>(this ILiveSubscription<TIState> subscription, bool block = true)
            where TIState : IState
        {
            return subscription.GetState(subscription.GetStateLock(block));
        }
    }
}
