using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public interface ILiveList<out T> : ILiveCollection<T, IListDelta<T>>
    {
    }

    public class LiveList<T> : LiveCollection<T, IList<T>, IListDelta<T>, ListDelta<T>, LiveListInner<T>, LiveList<T>>, ILiveList<T>
    {
        public LiveList()
            : this(new List<T>(), null)
        {
        }

        public LiveList(IList<T> publishCache, IEnumerable<T> inner)
            : base(publishCache, new List<T>(), inner)
        {
        }

        public static ILiveList<T> Empty = new T[0].ToLiveList();
        public static ILiveList<T> Default = new [] { default(T) }.ToLiveList();
    }

    public static partial class Extensions
    {
        public static LiveList<T> ToLiveList<T>(this IList<T> publishCache)
        {
            return new LiveList<T>(publishCache, publishCache);
        }
    }
}