using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveCollectionObservable<T, TIDelta, TICollection> : LiveCollectionView<T, TIDelta, TICollection>, IDisposable
        where TIDelta : ICollectionDelta<T>
        where TICollection : ICollection<T>
    {
        private readonly Func<Action, bool, IDisposable, ICollectionState<T, TIDelta>, ICollectionState<T, TIDelta>> _innerGetState;
        private readonly Action _onComplete;
        private ICollectionState<T, TIDelta> _last;

        public static ILiveCollection<T, TIDelta> Create(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, TIDelta>, ICollectionState<T, TIDelta>> innerGetState, Action onComplete)
        {
            return new LiveCollectionObservable<T, TIDelta, TICollection>(init, innerGetState, onComplete);
        }

        public static ILiveCollection<T, TIDelta> Create(Func<Action, ReaderWriterLocker> init, Func<Action, bool, ILock, ICollectionState<T, TIDelta>, ICollectionState<T, TIDelta>> innerGetState, Action onComplete)
        {
            return Create(innerChanged => new Locker(init(innerChanged)),
                          (innerChanged, notified, @lock, oldState) =>
                          innerGetState(innerChanged, notified, (ILock)@lock, oldState),
                          onComplete);
        }

        public LiveCollectionObservable(
            Func<Action, Locker> init,
            Func<Action, bool, IDisposable, ICollectionState<T, TIDelta>, ICollectionState<T, TIDelta>> innerGetState,
            Action onComplete)
        {
            _innerGetState = innerGetState;
            _onComplete = onComplete;

            // initialise (must set _innerStateLocker before return)
            Callback
                .PostponeInside(
                    InnerChanged, // => Publish.OnConsume(InnerChanged),
                    callback => _innerStateLocker = init(callback));
        }

        protected override ICollectionState<T, TIDelta> InnerGetState(bool notified, IDisposable stateLock)
        {
            return _last =
                _innerGetState(
                    InnerChanged, //() => Publish.OnConsume(InnerChanged),
                    notified,
                    stateLock,
                    _last);
        }

        protected override void OnCompleted()
        {
            _onComplete();
        }


        public void Dispose()
        {
            _onComplete();
        }
    }

    public class LiveCollectionObservable<T> : LiveCollectionObservable<T, ICollectionDelta<T>, ICollection<T>>, ILiveCollection<T>
    {
        new public static ILiveCollection<T> Create(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, ICollectionDelta<T>>, ICollectionState<T, ICollectionDelta<T>>> innerGetState, Action onComplete)
        {
            return new LiveCollectionObservable<T>(init, innerGetState, onComplete);
        }

        new public static ILiveCollection<T> Create(Func<Action, ReaderWriterLocker> init, Func<Action, bool, ILock, ICollectionState<T, ICollectionDelta<T>>, ICollectionState<T, ICollectionDelta<T>>> innerGetState, Action onComplete)
        {
            return Create(innerChanged => new Locker(init(innerChanged)),
                          (innerChanged, notified, @lock, oldState) =>
                          innerGetState(innerChanged, notified, (ILock)@lock, oldState),
                          onComplete);
        }

        protected LiveCollectionObservable(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, ICollectionDelta<T>>, ICollectionState<T, ICollectionDelta<T>>> innerGetState, Action onComplete)
            : base(init, innerGetState, onComplete)
        {
        }
    }
}
