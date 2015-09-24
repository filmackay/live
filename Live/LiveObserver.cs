using System;
using System.Diagnostics;

namespace Vertigo.Live
{
    public interface ILiveObserver<in TIState> : IDisposable
        where TIState : IState
    {
        void Bind(ILiveSubscription<TIState> subscription);
        void OnNotify();
    }

    public class LiveObserver<TIState> : ILiveObserver<TIState>
        where TIState : IState
    {
        public ILiveObservable<TIState> Observable { get; private set; }
        private readonly Action _onNotify;
        private ILiveSubscription<TIState> _subscription;
        public TIState Last { get; private set; }

        public LiveObserver(ILiveObservable<TIState> observable, Action onNotify)
        {
            Observable = observable;
            _onNotify = onNotify;
        }

        void ILiveObserver<TIState>.Bind(ILiveSubscription<TIState> subscription)
        {
            if (_subscription != null || subscription == null)
                throw new InvalidOperationException("Only a single bind allowed");
            _subscription = subscription;
        }

        void ILiveObserver<TIState>.OnNotify()
        {
            _onNotify();
        }

        public Locker GetStateLock { get { return _getStateLock; } }
        private IDisposable _getStateLock(bool block = true)
        {
            return _subscription.GetStateLock(block);
        }

        public TIState GetState(IDisposable stateLock)
        {
            return Last = _subscription.GetState(stateLock);
        }

        public void GetNotify()
        {
            _subscription.GetNotify();
        }

        public void Terminate()
        {
            _subscription.Terminate();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }

    public static partial class LiveObserver
    {
        public static TIState GetState<TIState>(this LiveObserver<TIState> observer)
            where TIState : IState
        {
            var stateLock = observer.GetStateLock();
            return observer.GetState(stateLock);
        }

        public static void UseState<TIState>(this LiveObserver<TIState> observer, Action<TIState> useChange)
            where TIState : IState
        {
            var state = observer.GetState();
            using (state as IDisposable)
                useChange(state);
        }
    }

    public static partial class LiveObservable
    {
        public static LiveObserver<TIState> CreateObserver<TIState>(this ILiveObservable<TIState> observable, Action onNotify)
            where TIState : IState
        {
            return new LiveObserver<TIState>(observable, onNotify);
        }

        public static LiveObserver<TIState> CreateObserver<TIState>(this ILiveObservable<TIState> observable, Action<LiveObserver<TIState>> onNotify)
            where TIState : IState
        {
            var observer = default(LiveObserver<TIState>);
            return observer = observable.CreateObserver(() => onNotify(observer));
        }
    }
}
