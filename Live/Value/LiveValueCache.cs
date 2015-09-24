using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace Vertigo.Live
{
    public class LiveValueWriter<T> : LiveValuePublisher<T>
    {
        public T PublishValue;
        private T _oldValue;
        private readonly IEqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

        public LiveValueWriter(T @default = default(T))
        {
            Connect(PublishValue = @default);
            LiveValueWriters.Add(CheckForUpdate);
        }

        public void CheckForUpdate()
        {
            if (_equalityComparer.Equals(PublishValue, _oldValue))
                return;

            _oldValue = PublishValue;
            SetValue(_oldValue = PublishValue);
        }
    }

    public static class LiveValueWriters
    {
        private static readonly List<Action> _writers = new List<Action>();
        private static readonly Subject<Unit> _checkForUpdates = new Subject<Unit>();

        static LiveValueWriters()
        {
            _checkForUpdates
                .SimpleThrottle(TimeSpan.FromSeconds(0.1))
                .Subscribe(unit =>
                    {
                        Action[] writers;
                        lock (_writers)
                            writers = _writers.ToArray();
                        using (Publish.Transaction())
                            writers.ForEach();
                    });
        }

        public static void Add(Action checkForUpdate)
        {
            lock(_writers)
                _writers.Add(checkForUpdate);
        }

        public static void CheckForUpdates()
        {
            _checkForUpdates.OnNext(Unit.Default);
        }
    }

    public static partial class LiveValue
    {
        public static ILiveValue<T> ToReader<T>(this ILiveValue<T> source)
        {
            var observer = default(LiveObserver<IValueState<T>>);
            var last = (IValueState<T>)new ValueState<T>();

            return LiveValueObservable<T>.Create(
                innerChanged =>
                    {
                        Action<LiveObserver<IValueState<T>>> action = o =>
                            {
                                var state = o.GetState();
                                if (state.Status.IsConnected())
                                    last = state;
                                innerChanged();
                            };

                        Callback.Split(
                            action, // inside
                            o => Publish.OnPublishConsume(() => action(o)),  // outside
                            callback => source.Subscribe(observer = source.CreateObserver(callback))
                            );
                    },
                () => observer.GetNotify(),
                (innerChanged, oldState) => oldState.Add(last),
                () => observer.Dispose());
        }
    }
}