using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> Skip<T>(this ILiveList<T> sourceList, ILiveValue<int> sourceCount)
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
                    var skipState = observerCount.GetState();
                    using (var stateList = observerList.GetState(stateLock))
                    {
                        // work out status
                        var status = skipState.Status.And(stateList.Status);

                        // work out delta
                        var oldSkip = skipState.OldValue;
                        var newSkip = skipState.NewValue;
                        ListDelta<T> delta = null;
                        if (status == StateStatus.Connected && (skipState.HasChange || stateList.Delta.HasChange()))
                        {
                            delta = new ListDelta<T>();
                            var node = stateList.Delta.IndexDeltas.First();

                            // take common range changes
                            while (node != null && node.Index < Math.Min(oldSkip, newSkip))
                            {
                                // isolate relevant part
                                node = node.SplitAt(Math.Min(oldSkip, newSkip) - node.Index);

                                // adjust for net changes
                                oldSkip += node.Data.InsertItems.Count() - node.Data.DeleteItems.Count();

                                // move on
                                node = node.Next;
                            }

                            // go through updates in cross-over zone
                            while (node != null && node.Index < Math.Max(newSkip, oldSkip))
                            {
                                // isolate relevant part
                                node = node.SplitAt(Math.Abs(newSkip - oldSkip));
                                var nodeChange = node.Data.InsertItems.Count() - node.Data.DeleteItems.Count();

                                // pass-through from state.Inner where no index delta applies
                                if (oldSkip < newSkip)
                                {
                                    var deleteCount = node.Index - oldSkip;
                                    if (deleteCount > 0)
                                        delta.Delete(0, stateList.Inner.Skip(oldSkip).Take(deleteCount));

                                    // adjust old view relative to new changes
                                    oldSkip += nodeChange;
                                }
                                else if (oldSkip > newSkip)
                                {
                                    // merge deletes from node, and (offsetting) inserts from revelation
                                    var revelationCount = oldSkip - node.Index;
                                    var nodeDeleteCount = node.Data.DeleteItems.Count();
                                    var offsetCount = Math.Min(revelationCount, nodeDeleteCount);
                                    var netChange = revelationCount - nodeDeleteCount;

                                    if (netChange > 0)
                                        delta.Insert(node.Index - newSkip,
                                                     stateList.Inner.Skip(node.Index + nodeChange).Take(netChange));
                                    else if (netChange < 0)
                                        delta.Delete(node.Index - newSkip, node.Data.DeleteItems.Skip(offsetCount));

                                    // pass through index delta changes
                                    delta.Insert(node.Index - newSkip, node.Data.InsertItems);

                                    oldSkip -= revelationCount;
                                }

                                node = node.Next;
                            }

                            // apply remaining cross-over range from state.Inner
                            var count = -newSkip + oldSkip;
                            if (count < 0)
                                delta.Delete(0, stateList.Inner.Skip(oldSkip).Take(-count));
                            else if (count > 0)
                                delta.Insert(0, stateList.Inner.Skip(newSkip).Take(count));
                            oldSkip += -count;

                            // pass through remaining updates
                            while (node != null)
                            {
                                var index = node.Index - newSkip;
                                delta.Delete(index, node.Data.DeleteItems);
                                delta.Insert(index, node.Data.InsertItems);
                                node = node.Next;
                            }

                            Debug.Assert(oldSkip == newSkip);
                        }

                        var ret = new CollectionState<T, IListDelta<T>, IList<T>>();
                        ret.SetState(status,
                                     delta,
                                     stateList.Inner.Skip(skipState.NewValue),
                                     Math.Max(stateList.LastUpdated, skipState.LastUpdated),
                                     stateList.InnerSourceLock());
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
