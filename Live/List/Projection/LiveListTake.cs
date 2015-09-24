using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Take<T>(this ILiveList<T> sourceList, ILiveValue<int> sourceCount)
        {
            LiveObserver<ICollectionState<T, IListDelta<T>>> observerList = null;
            LiveObserver<IValueState<int>> observerCount = null;

            return LiveListObservable<T>.Create(
                innerChanged =>
                {
                    sourceList.Subscribe(observerList = sourceList.CreateObserver(innerChanged));
                    sourceCount.Subscribe(observerCount = sourceCount.CreateObserver(innerChanged));
                    return observerList.GetStateLock;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get source change
                    var stateCount = observerCount.GetState();
                    using (var stateList = observerList.GetState(stateLock))
                    {
                        // work out status
                        var status = stateCount.Status.And(stateList.Status);

                        // work out delta
                        var oldTake = stateCount.OldValue;
                        var newTake = stateCount.NewValue;
                        ListDelta<T> delta = null;
                        if (status == StateStatus.Connected && (stateCount.HasChange || stateList.Delta.HasChange()))
                        {
                            delta = new ListDelta<T>();
                            var node = stateList.Delta.IndexDeltas.First();

                            // take common range changes
                            while (node != null && node.Index < Math.Min(oldTake, newTake))
                            {
                                // isolate relevant part
                                node = node.SplitAt(Math.Min(oldTake, newTake) - node.Index);

                                // apply deletes
                                var deleteCount = node.Data.DeleteItems.Count();
                                if (deleteCount > 0)
                                {
                                    delta.Delete(node.Index, node.Data.DeleteItems);
                                    oldTake -= deleteCount;
                                }

                                // apply inserts
                                var insertCount = node.Data.InsertItems.Count();
                                if (insertCount > 0)
                                {
                                    delta.Insert(node.Index, node.Data.InsertItems);
                                    oldTake += insertCount;
                                }

                                node = node.Next;
                            }

                            // newly exposed range?
                            if (newTake > oldTake) // new range
                            {
                                var insertCount = newTake - oldTake;
                                delta.Insert(oldTake, stateList.Inner.Skip(oldTake).Take(insertCount));
                                oldTake += insertCount;
                            }
                            // old range we are discarding?
                            else if (newTake < oldTake)
                            {
                                // go through updates looking for deletes that wont be in state.Inner
                                var newInserts = 0;
                                while (node != null && node.Index <= oldTake)
                                {
                                    // remove from state.Inner where no index delta applies
                                    var deleteCount = node.Index - (newTake + newInserts);
                                    if (deleteCount > 0)
                                    {
                                        delta.Delete(newTake, stateList.Inner.Skip(newTake + newInserts).Take(deleteCount));
                                        oldTake -= deleteCount;
                                        newInserts += deleteCount;
                                    }

                                    // isolate relevant part
                                    node = node.SplitAt(oldTake - newTake);

                                    // deletes from index delta
                                    delta.Delete(newTake, node.Data.DeleteItems);
                                    oldTake -= node.Data.DeleteItems.Count();

                                    // skip over inserted items
                                    newInserts += node.Data.InsertItems.Count();

                                    node = node.Next;
                                }

                                // remove remaining range from state.Inner
                                var delCount = oldTake - newTake;
                                if (delCount > 0)
                                {
                                    delta.Delete(newTake, stateList.Inner.Skip(newTake + newInserts).Take(delCount));
                                    oldTake -= delCount;
                                    newInserts += delCount;
                                }
                            }

                            Debug.Assert(oldTake == newTake);
                        }

                        var ret = new CollectionState<T, IListDelta<T>, IList<T>>();
                        ret.SetState(status,
                            delta,
                            stateList.Inner.Take(stateCount.NewValue),
                            Math.Max(stateList.LastUpdated, stateCount.LastUpdated),
                            stateList.InnerSourceLock(true));
                        return ret;
                    }
                },
                () =>
                {
                    observerList.Dispose();
                    observerCount.Dispose();
                });
        }
    }
}
