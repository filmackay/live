using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Vertigo.Live
{
    public interface ILiveDictionary<TKey, TValue> : ILiveCollection<KeyValuePair<TKey, TValue>, IDictionaryDelta<TKey, TValue>>
    {
        ILiveValue<TValue> this[ILiveValue<TKey> key] { get; }
    }

    public class LiveDictionary<TKey, TValue> : LiveCollection<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>, IDictionaryDelta<TKey, TValue>, DictionaryDelta<TKey, TValue>, LiveDictionaryInner<TKey, TValue>, LiveDictionary<TKey, TValue>>, ILiveDictionary<TKey, TValue>
    {
        public LiveDictionary()
            : this(new Dictionary<TKey, TValue>(), null)
        {
        }

        public LiveDictionary(IDictionary<TKey, TValue> publishCache, IEnumerable<KeyValuePair<TKey,TValue>> inner)
            : base(publishCache, new Dictionary<TKey, TValue>(), inner)
        {
        }

        public ILiveValue<TValue> this[ILiveValue<TKey> key]
        {
            get { return this.Value(key); }
        }
    }

    public static partial class Extensions
    {
        public static LiveDictionary<TKey, TValue> ToLiveDictionary<TKey, TValue>(this IDictionary<TKey, TValue> publishCache)
        {
            return new LiveDictionary<TKey, TValue>(publishCache, publishCache);
        }

        public static TResult UseInner<TKey, TValue, TResult>(this ILiveDictionary<TKey, TValue> source, Func<IDictionary<TKey, TValue>, TResult> use)
        {
            var ret = default(TResult);
            source
                .States()
                .Take(1)
                .ForEach(state => ret = use(state.Inner as IDictionary<TKey, TValue>));
            return ret;
        }
    }
}
