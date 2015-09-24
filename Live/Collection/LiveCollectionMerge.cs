using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveCollection<T> Merge<T, TIDelta1, TIDelta2>(this ILiveCollection<T, TIDelta1> source1, ILiveCollection<T, TIDelta2> source2)
            where TIDelta1 : class, ICollectionDelta<T>
            where TIDelta2 : class, ICollectionDelta<T>
        {
            LiveObserver<ICollectionState<T, TIDelta1>> observer1 = null;
            LiveObserver<ICollectionState<T, TIDelta2>> observer2 = null;

            return LiveCollectionObservable<T>.Create(
                innerChanged =>
                {
                    source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
                    source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
                    return new[] { observer1.GetStateLock, observer2.GetStateLock }.MergeLockers();
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get states
                    var locks = ((CompositeDisposable)stateLock).ToArray();
                    var state1 = observer1.GetState(locks[0]);
                    var state2 = observer2.GetState(locks[1]);

                    // prepare delta
                    CollectionDelta<T> delta = null;
                    if (state1.Status == StateStatus.Connected &&
                        state2.Status == StateStatus.Connected &&
                        (state1.Delta.HasChange() || state2.Delta.HasChange()))
                    {
                        delta = new CollectionDelta<T>();
                        delta.Add(state1.Delta);
                        delta.Add(state2.Delta);
                    }

                    // detach source locks
                    state1.InnerSourceLock();
                    state2.InnerSourceLock();

                    // prepare result state
                    var result = new CollectionState<T, ICollectionDelta<T>, ICollection<T>>();
                    result.SetState(state1.Status.And(state2.Status),
                                    delta,
                                    state1.Inner.Concat(state2.Inner),
                                    Math.Max(state1.LastUpdated, state2.LastUpdated),
                                    stateLock);
                    return result;
                },
                () =>
                {
                    observer1.Dispose();
                    observer2.Dispose();
                });
        }
    }
}
