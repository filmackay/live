using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<Tuple<T1, T2>> Zip<T1, T2>(this ILiveList<T1> source1, ILiveList<T2> source2)
        {
            LiveObserver<ICollectionState<T1, IListDelta<T1>>> observer1 = null;
            LiveObserver<ICollectionState<T2, IListDelta<T2>>> observer2 = null;
            var cache = new CollectionStateCache<Tuple<T1, T2>, IList<Tuple<T1, T2>>, IListDelta<Tuple<T1, T2>>>(new List<Tuple<T1, T2>>());

            return LiveListObservable<Tuple<T1, T2>>.Create(
                innerChanged =>
                {
                    source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
                    source2.Subscribe(observer2 = source2.CreateObserver(innerChanged));
                    return null; // cache.WriteLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    if (notified)
                    {
                        using (var l = new[] {observer1.GetStateLock, observer2.GetStateLock}.LockAll())
                        {
                            var locks = l.ToArray();
                            using (var state1 = observer1.GetState(locks[0]))
                            using (var state2 = observer2.GetState(locks[1]))
                            {
                                var status = state1.Status.And(state2.Status);

                                // work out delta
                                ListDelta<Tuple<T1, T2>> delta = null;
                                if (status == StateStatus.Connected)
                                {
                                    delta = new ListDelta<Tuple<T1, T2>>();

                                    // go through both changes
                                    var enumA = state1.Delta.IndexDeltas.GetEnumerator();
                                    var enumB = state2.Delta.IndexDeltas.GetEnumerator();
                                    //var oldIndex = Math.Min(enumA.AtEnd ? int.MaxValue : enumA.Current.Index,
                                    //                        enumB.AtEnd ? int.MaxValue : enumB.Current.Index);
                                    //var newIndex = oldIndex;
                                    while (true)
                                    {
                                        // TODO: complete
                                    }
                                }

                                // apply to cache
                                cache.AddState(
                                    status,
                                    state1.Inner.Zip(state2.Inner, Tuple.Create),
                                    delta,
                                    Math.Max(state1.LastUpdated, state2.LastUpdated),
                                    false);
                            }
                        }
                    }
                    stateLock.DowngradeToReader();
                    return cache.Copy(stateLock);
                },
                () =>
                {
                    observer1.Dispose();
                    observer2.Dispose();
                });
        }
    }
}
