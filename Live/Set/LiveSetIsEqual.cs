﻿using System;
using System.Linq;
using System.Reactive.Disposables;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveValue<bool> IsEqual<T>(this ILiveSet<T> source1, ILiveSet<T> source2)
        {
            LiveObserver<ICollectionState<T, ISetDelta<T>>> observer1 = null;
            LiveObserver<ICollectionState<T, ISetDelta<T>>> observer2 = null;
            var result = new ValueState<bool>();

            return LiveValueObservable<bool>.Create(
                innerChanged =>
                {
                    source1.Subscribe(observer1 = source1.CreateObserver(innerChanged));
                    source2.Subscribe(observer2 = source1.CreateObserver(innerChanged));
                },
                () =>
                {
                    observer1.GetNotify();
                    observer2.GetNotify();
                },
                (innerChanged, oldState) =>
                {
                    // get states
                    var stateLocks = new[] { observer1.GetStateLock, observer2.GetStateLock }.LockAll2();
                    using (var state1 = observer1.GetState(stateLocks[0]))
                    using (var state2 = observer2.GetState(stateLocks[1]))
                    {
                        result.Status = result.Status.AddSimple(state1.Status.And(state2.Status));
                        result.OldValue = result.NewValue;

                        if (result.Status != StateStatus.Disconnected)
                        {
                            if (result.NewValue)
                            {
                                // historically equal - confirm deltas are the same
                                result.NewValue = state1.Delta.DeltaEquals(state2.Delta);
                            }
                            else
                            {
                                // historically unequal
                                result.NewValue = state1.Inner.UnorderedEqual(state2.Inner);
                            }

                            result.LastUpdated = Math.Max(state1.LastUpdated, state2.LastUpdated);
                        }
                    }

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