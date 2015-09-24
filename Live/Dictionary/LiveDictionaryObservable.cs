using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveDictionaryObservable<TKey, TValue> : LiveCollectionObservable<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>, IDictionary<TKey, TValue>>, ILiveDictionary<TKey, TValue>
    {
        new public static ILiveDictionary<TKey, TValue> Create(
            Func<Action, Locker> init,
            Func<Action, bool, IDisposable, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> innerGetState,
            Action onComplete)
        {
            return new LiveDictionaryObservable<TKey, TValue>(init, innerGetState, onComplete);
        }

        new public static ILiveDictionary<TKey, TValue> Create(
            Func<Action, ReaderWriterLocker> init,
            Func<Action, bool, ILock, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> innerGetState,
            Action onComplete)
        {
            return Create(innerChanged => new Locker(init(innerChanged)),
                          (innerChanged, notified, @lock, oldState) =>
                          innerGetState(innerChanged, notified, (ILock)@lock, oldState),
                          onComplete);
        }

        public LiveDictionaryObservable(
            Func<Action, Locker> init,
            Func<Action, bool, IDisposable, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>, ICollectionState<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>> innerGetState,
            Action onComplete)
            : base(init, innerGetState, onComplete)
        {
        }

        public ILiveValue<TValue> this[ILiveValue<TKey> key]
        {
            get { return this.Value(key); }
        }
    }
}
