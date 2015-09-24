using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveList<int> Range(ILiveValue<int> startValue, ILiveValue<int> countValue)
        {
            LiveObserver<IValueState<int>> startObserver = null;
            LiveObserver<IValueState<int>> countObserver = null;

            return LiveListObservable<int>.Create(
                innerChanged =>
                {
                    countValue.Subscribe(countObserver = countValue.CreateObserver(innerChanged));
                    startValue.Subscribe(startObserver = startValue.CreateObserver(innerChanged));
                    return Lockers.Empty;
                },
                (innerChanged, notified, stateLock, oldState) =>
                {
                    // get state
                    var count = countObserver.GetState();
                    var start = startObserver.GetState();
                    var newStatus = count.Status.And(start.Status);

                    // work out delta
                    ListDelta<int> delta = null;
                    if (newStatus.IsDeltaRelevant() && (count.HasChange || start.HasChange))
                    {
                        delta = new ListDelta<int>();

                        // adjust head
                        var head = Math.Min(Math.Max(start.OldValue - start.NewValue, -count.OldValue), count.NewValue);
                        if (head < 0)
                            delta.Delete(0, Enumerable.Range(start.OldValue, -head));
                        else if (head > 0)
                            delta.Insert(0, Enumerable.Range(start.NewValue, head));
                        var currentCount = count.OldValue + head;
                        var leftoverCount = count.OldValue + Math.Min(0, head);

                        // adjust tail
                        var tail = count.NewValue - currentCount;
                        if (tail > 0)
                            delta.Insert(currentCount, Enumerable.Range(start.NewValue + currentCount, tail));
                        else if (tail < 0)
                            delta.Delete(count.NewValue, Enumerable.Range(start.OldValue + count.OldValue + tail, -tail));
                    }

                    // work out new state
                    var result = new CollectionState<int, IListDelta<int>, IList<int>>();
                    result.SetState(
                        oldState.GetStatus().Add(newStatus),
                        delta,
                        Enumerable.Range(start.NewValue, count.NewValue),
                        Math.Max(count.LastUpdated, start.LastUpdated),
                        stateLock);
                    return result;
                },
                () =>
                {
                    startObserver.Dispose();
                    countObserver.Dispose();
                });
        }
    }
}
