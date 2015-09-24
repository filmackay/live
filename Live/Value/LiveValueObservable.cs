using System;

namespace Vertigo.Live
{
    public class LiveValueObservable<T> : LiveValue<T>, IDisposable
    {
        private readonly Func<Action, IValueState<T>, IValueState<T>> _getState;
        private readonly Action _getNotify;
        private readonly Action _onComplete;

        public static ILiveValue<T> Create(Action<Action> init, Action getNotify, Func<Action, IValueState<T>, IValueState<T>> getState, Action onComplete)
        {
            return new LiveValueObservable<T>(init, getNotify, getState, onComplete);
        }

        public LiveValueObservable(Action<Action> init, Action getNotify, Func<Action, IValueState<T>, IValueState<T>> innerGetState, Action onComplete)
        {
            _getNotify = getNotify;
            _getState = innerGetState;
            _onComplete = onComplete;

            Callback
                .PostponeInside(
                    InnerChanged, //() => Publish.OnConsume(InnerChanged),
                    init);
        }

        public override void InnerGetNotify()
        {
            _getNotify();
        }

        public override void InnerGetValue(ref T value, ref StateStatus status, ref long lastUpdated)
        {
            var oldState = new ValueState<T> { LastUpdated = lastUpdated, NewValue = value, Status = status };

            var newState =
                _getState(
                    InnerChanged, //() => Publish.OnConsume(InnerChanged),
                    oldState);

            value = newState.NewValue;
            status = status.AddSimple(newState.Status);
            lastUpdated = newState.LastUpdated;

            if (status == StateStatus.Completing)
                _onComplete();
        }

        public void Dispose()
        {
            _onComplete();
        }
    }
}
