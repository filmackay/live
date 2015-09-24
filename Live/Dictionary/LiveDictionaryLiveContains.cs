using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveValue<bool> Contains<TKey, TElement>(this ILiveDictionary<TKey, TElement> source, ILiveValue<TKey> key)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TElement>, IDictionaryDelta<TKey, TElement>>> observer = null;
            LiveObserver<IValueState<TKey>> keyObserver = null;

            return LiveValueObservable<bool>.Create(
                innerChanged =>
                {
                    source.Subscribe(observer = source.CreateObserver(innerChanged));
                    key.Subscribe(keyObserver = key.CreateObserver(innerChanged));
                },
                () =>
                {
                    observer.GetNotify();
                    keyObserver.GetNotify();
                },
                (innerChanged, oldState) =>
                {
                    using (var state = observer.GetState())
                    {
                        var itemChange = keyObserver.GetState();
                        var result = new ValueState<bool>
                        {
                            Status = itemChange.Status.And(state.Status),
                            LastUpdated = state.LastUpdated,
                        };
                        result.NewValue = result.Status == StateStatus.Disconnected
                            ? false
                            : ((IDictionary<TKey, TElement>)state.Inner).ContainsKey(itemChange.NewValue);
                        return result;
                    }
                },
                () =>
                {
                    observer.Dispose();
                    keyObserver.Dispose();
                });
        }
    }
}