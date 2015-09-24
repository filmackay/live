using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    /*
    public class LiveKeyValuePairsValuesView<TKey, TValue, TIDelta> : LiveCollectionView<TValue>
        where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, TValue>>
    {
        public LiveKeyValuePairsValuesView(ILiveCollection<KeyValuePair<TKey, TValue>, TIDelta> source)
        {
            _subscription = source.Subscribe();
            _innerStateLocker = _subscription.GetStateLock;
            _subscription.ConnectAndNotify(InnerChanged);
        }

        private readonly ISubscription<ICollectionState<KeyValuePair<TKey, TValue>, TIDelta>> _subscription;

        protected override CollectionState<TValue, ICollection<TValue>, ICollectionDelta<TValue>, CollectionDelta<TValue>> InnerGetState(bool notified, IDisposable stateLock)
        {
            return _subscription
                .GetState(stateLock)
                .Extract<TValue, ICollection<TValue>, ICollectionDelta<TValue>, CollectionDelta<TValue>>
                    (true,
                     (inner, delta) => delta.ToCollectionDelta(items => items.Select(kv => kv.Value)), inner => Enumerable.Select(inner, kv => kv.Value));
        }

        protected override void DisposeOrFinalize(bool disposing)
        {
            _subscription.Complete();
        }
    }

    public static partial class Extensions
    {
        public static ILiveCollection<TValue> Values<TKey, TValue, TIDelta>(this ILiveCollection<KeyValuePair<TKey, TValue>, TIDelta> source)
            where TIDelta : class, ICollectionDelta<KeyValuePair<TKey, TValue>>
        {
            return new LiveKeyValuePairsValuesView<TKey, TValue, TIDelta>(source);
        }
    }*/
}