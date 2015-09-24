using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveListObservable<T> : LiveCollectionObservable<T, IListDelta<T>, IList<T>>, ILiveList<T>
    {
        new public static ILiveList<T> Create(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, IListDelta<T>>, ICollectionState<T, IListDelta<T>>> innerGetState, Action onComplete)
        {
            return new LiveListObservable<T>(init, innerGetState, onComplete);
        }

        new public static ILiveList<T> Create(Func<Action, ReaderWriterLocker> init, Func<Action, bool, ILock, ICollectionState<T, IListDelta<T>>, ICollectionState<T, IListDelta<T>>> innerGetState, Action onComplete)
        {
            return Create(innerChanged => new Locker(init(innerChanged)),
                          (innerChanged, notified, @lock, oldState) =>
                          innerGetState(innerChanged, notified, (ILock)@lock, oldState),
                          onComplete);
        }

        protected LiveListObservable(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, IListDelta<T>>, ICollectionState<T, IListDelta<T>>> innerGetState, Action onComplete)
            : base(init, innerGetState, onComplete)
        {
        }
    }
}
