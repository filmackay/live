using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Concat<T>(this ILiveList<T> source1, ILiveList<T> source2)
        {
            LiveObserver<ICollectionState<T, IListDelta<T>>> observer1 = null;
            LiveObserver<ICollectionState<T, IListDelta<T>>> observer2 = null;
            var subscription1Count = 0;

            return LiveListObservable<T>.Create(
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

                    // initialize count
                    if (state1.Status == StateStatus.Connecting)
                        subscription1Count = state1.Inner.Count();

                    // prepare delta
                    ListDelta<T> delta = null;
                    var state = state1.Status.And(state2.Status);
                    if (state1.Delta != null || state2.Delta != null)
                    {
                        delta = new ListDelta<T>();

                        // copy first source
                        if (state1.Delta != null)
                        {
                            delta.Add(state1.Delta);
                            subscription1Count += (state1.Delta.Inserts == null ? 0 : state1.Delta.Inserts.Count()) - (state1.Delta.Deletes == null ? 0 : state1.Delta.Deletes.Count());
                        }

                        // offset second source
                        if (state2.Delta != null)
                        {
                            foreach (var indexDelta in state2.Delta.IndexDeltas)
                            {
                                delta.Delete(indexDelta.Index + subscription1Count, indexDelta.Data.DeleteItems);
                                delta.Insert(indexDelta.Index + subscription1Count, indexDelta.Data.InsertItems);
                            }
                        }
                    }

                    // detach source locks
                    state1.InnerSourceLock();
                    state2.InnerSourceLock();

                    // prepare result state
                    var result = new CollectionState<T, IListDelta<T>, IList<T>>();
                    result.SetState(state,
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
