using System;
using System.Collections.Generic;
using System.Windows;

namespace Vertigo.Live
{
    public interface ILiveSet<out T> : ILiveCollection<T, ISetDelta<T>>
    {
    }

    public class LiveSet<T, TLiveSetInner, TLiveSet> : LiveCollection<T, ISet<T>, ISetDelta<T>, SetDelta<T>, TLiveSetInner, TLiveSet>, ILiveSet<T>
        where TLiveSetInner : LiveCollectionInner<T, ISet<T>, ISetDelta<T>, SetDelta<T>, TLiveSet>, ISet<T>, new()
        where TLiveSet :  LiveSet<T, TLiveSetInner, TLiveSet>
    {
        public LiveSet(ISet<T> publishCache, ISet<T> innerCache, IEnumerable<T> inner)
            : base(publishCache, innerCache, inner)
        {
        }
    }

    public class LiveSet<T> : LiveSet<T, LiveSetInner<T>, LiveSet<T>>
    {
        public static ILiveSet<T> Empty = new HashSet<T>().ToLiveSet();

        public LiveSet()
            : this(new HashSet<T>(), null)
        {
        }

        public LiveSet(ISet<T> publishCache, IEnumerable<T> inner)
            : base(publishCache, new HashSet<T>(), inner)
        {
        }
    }

    public static partial class Extensions
    {
        public static LiveSet<T> ToLiveSet<T>(this ISet<T> publishCache)
        {
            return new LiveSet<T>(publishCache, publishCache);
        }

        public static ILiveSet<T> ToILiveSet<T>(this ISet<T> publishCache)
        {
            return publishCache.ToLiveSet() as ILiveSet<T>;
        }
    }
}
