using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<T> DistinctUntilChanged<T, TKey>(this ILiveValue<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            equalityComparer = equalityComparer ?? EqualityComparer<TKey>.Default;
            var gate = new object();
            var observer = default(LiveObserver<IValueState<T>>);
            var lastState = default(IValueState<T>);
            var lastKey = default(TKey);

            return
                LiveValueObservable<T>.Create(
                    innerChanged =>
                        // subscribe to source and process items
                        Callback.Split<LiveObserver<IValueState<T>>>(
                            o => // inside
                                innerChanged(),
                            o => // outside
                                Publish.OnPublishConsume(
                                    () =>
                                    {
                                        using (gate.Lock())
                                        {
                                            // get state
                                            var newState = o.GetState();

                                            // value changed?
                                            var newKey = lastState.Status.IsInnerRelevant() ? keySelector(newState.NewValue) : default(TKey);
                                            if (lastState.Status.Next() == lastState.Status.AddSimple(newState.Status) &&
                                                equalityComparer.Equals(lastKey, newKey))
                                                return;

                                            // we have a new value to publish
                                            lastKey = newKey;
                                            lastState = newState;
                                        }

                                        // notify
                                        innerChanged();
                                    }),
                            callback => source.Subscribe(observer = source.CreateObserver(callback))),
                    () => observer.GetNotify(),
                    (innerChanged, oldState) =>
                    {
                        using (gate.Lock())
                        {
                            var state = observer.GetState();
                            if (lastState == null)
                                lastState = state;
                            else
                                state = lastState.Add(state);
                            return oldState.Add(lastState.Add(state));
                        }
                    },
                    () => observer.Dispose());
        }

        public static ILiveValue<T> DistinctUntilChanged<T>(this ILiveValue<T> source, IEqualityComparer<T> equalityComparer = null)
        {
            return source.DistinctUntilChanged(i => i, equalityComparer);
        }
    }
}
