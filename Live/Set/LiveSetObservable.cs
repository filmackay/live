using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Linq;

namespace Vertigo.Live
{
    public class LiveSetObservable<T> : LiveCollectionObservable<T, ISetDelta<T>, ISet<T>>, ILiveSet<T>
    {
        new public static ILiveSet<T> Create(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, ISetDelta<T>>, ICollectionState<T, ISetDelta<T>>> innerGetState, Action onComplete)
        {
            return new LiveSetObservable<T>(init, innerGetState, onComplete);
        }

        new public static ILiveSet<T> Create(Func<Action, ReaderWriterLocker> init, Func<Action, bool, ILock, ICollectionState<T, ISetDelta<T>>, ICollectionState<T, ISetDelta<T>>> innerGetState, Action onComplete)
        {
            return Create(innerChanged => new Locker(init(innerChanged)),
                          (innerChanged, notified, @lock, oldState) =>
                          innerGetState(innerChanged, notified, (ILock)@lock, oldState),
                          onComplete);
        }

        public LiveSetObservable(Func<Action, Locker> init, Func<Action, bool, IDisposable, ICollectionState<T, ISetDelta<T>>, ICollectionState<T, ISetDelta<T>>> innerGetState, Action onComplete)
            : base(init, innerGetState, onComplete)
        {
        }
    }
}
