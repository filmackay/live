using System;
using System.Collections.Generic;
using System.Linq;

/*
namespace Vertigo.Live
{
    public class LiveCollectionPermutations<T, TIDelta> : LiveCollectionView<Tuple<T, T>>
        where TIDelta : class, ICollectionDelta<T>
    {
        public LiveCollectionPermutations(ILiveCollection<T, TIDelta> source)
        {
            _subscription = source.Subscribe();
            if (_subscription.Start(InnerChanged))
                InnerChanged();
            _innerStateLocker = _subscription.GetStateLock;
        }

        private ISubscription<ICollectionState<T, TIDelta>> _subscription;

        protected override CollectionState<Tuple<T, T>, ICollection<Tuple<T, T>>, ICollectionDelta<Tuple<T, T>>, CollectionDelta<Tuple<T, T>>> InnerGetState(bool notified, IDisposable stateLock)
        {
            // get states
            var state = _subscription.GetState(stateLock);

            // prepare delta
            CollectionDelta<Tuple<T, T>> delta;
            if (state.HasDelta)
            {
                // cross-join the deltas
                delta = new CollectionDelta<Tuple<T, T>>();

                // get sequences of all items (past and present)
                var all = state.Inner;
                if (state.Delta != null)
                    all = all.Concat(state.Delta.Deletes);

                if (state.Delta != null)
                {
                    // apply inserts
                    delta.Insert(-1, state.Delta.Inserts.CrossJoin(all));
                    delta.Insert(-1, all.CrossJoin(state.Delta.Inserts));
                    delta.Delete(-1, state.Delta.Inserts.Permutations());

                    // apply deletes
                    delta.Delete(-1, state.Delta.Deletes.CrossJoin(all));
                    delta.Delete(-1, all.CrossJoin(state.Delta.Deletes));
                    delta.Insert(-1, state.Delta.Deletes.Permutations());
                }
            }
            else
                delta = null;

            // prepare result state
            var result = new CollectionState<Tuple<T, T>, ICollection<Tuple<T, T>>, ICollectionDelta<Tuple<T, T>>, CollectionDelta<Tuple<T, T>>>();
            result.SetState(_status.Add(state.Status),
                            delta,
                            state.Inner.Permutations(), stateLock);
            return result;
        }
    }

    public static partial class Extensions
    {
        public static ILiveCollection<Tuple<T, T>> Permutations<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : class, ICollectionDelta<T>
        {
            return new LiveCollectionPermutations<T, TIDelta>(source);
        }
    }
}*/