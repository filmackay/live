using System;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveList<T> TakeLast<T>(this ILiveList<T> list, ILiveValue<int> count)
        {
            return list.Reverse().Take(count);
        }
    }
}
