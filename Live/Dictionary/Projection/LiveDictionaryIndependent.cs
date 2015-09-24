using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveDictionary
    {
        public static ILiveDictionary<TKey, TElement> ToIndependent<TKey, TElement>(this ILiveDictionary<TKey, TElement> source)
        {
            LiveObserver<ICollectionState<KeyValuePair<TKey, TElement>, IDictionaryDelta<TKey, TElement>>> observer = null;
            IDisposable subscription = null;

            return LiveDictionaryObservable<TKey, TElement>.Create(
                innerChanged =>
                {
                    subscription = source.Subscribe(observer = source.CreateObserver(innerChanged));
                    return observer.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get state
                    using (var state = observer.GetState(stateLock))
                    {
                        // create independent copy
                        var independentState = new CollectionState<KeyValuePair<TKey, TElement>, IDictionaryDelta<TKey, TElement>, IDictionary<TKey, TElement>>();
                        independentState.SetState(state.Status,
                            state.Delta,
                            state.Inner.ToArray(),
                            state.LastUpdated,
                            null);
                        return independentState;
                    }
                },
                () => observer.Dispose());
        }
    }
}
