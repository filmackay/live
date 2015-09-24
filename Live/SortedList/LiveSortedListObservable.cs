using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveSortedListObservable<TKey, TValue> : LiveCollectionObservable<KeyValuePair<TKey, TValue>, IListDelta<KeyValuePair<TKey, TValue>>, IList<KeyValuePair<TKey, TValue>>>, ILiveSortedList<TKey, TValue>
    {
        new public static ILiveSortedList<TKey, TValue> Create(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<KeyValuePair<TKey, TValue>, IListDelta<KeyValuePair<TKey, TValue>>>, ICollectionState<KeyValuePair<TKey, TValue>, IListDelta<KeyValuePair<TKey, TValue>>>> innerGetState, Action onComplete)
        {
            return new LiveSortedListObservable<TKey, TValue>(init, innerGetState, onComplete);
        }

        new public static ILiveSortedList<TKey, TValue> Create(Func<Action, Locker[]> init, Func<Action, bool, IDisposable[], ICollectionState<KeyValuePair<TKey, TValue>, IListDelta<KeyValuePair<TKey, TValue>>>, ICollectionState<KeyValuePair<TKey, TValue>, IListDelta<KeyValuePair<TKey, TValue>>>> innerGetState, Action onComplete)
        {
            return Create(
                innerChanged => init(innerChanged).MergeLockers(),
                (innerChanged, notified, stateLock, oldState) => innerGetState(innerChanged, notified, ((CompositeDisposable)stateLock).ToArray(), oldState),
                onComplete);
        }

        public LiveSortedListObservable(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<KeyValuePair<TKey,TValue>, IListDelta<KeyValuePair<TKey,TValue>>>, ICollectionState<KeyValuePair<TKey,TValue>, IListDelta<KeyValuePair<TKey,TValue>>>> innerGetState, Action onComplete)
            : base(init, innerGetState, onComplete)
        {
        }
    }
}
