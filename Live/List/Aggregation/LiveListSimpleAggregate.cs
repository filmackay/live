using System;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveValue<T> First<T>(this ILiveList<T> source)
        {
            return source.Aggregate(Enumerable.First);
        }

        public static ILiveValue<T> FirstOrDefault<T>(this ILiveList<T> source)
        {
            return source.Aggregate(Enumerable.FirstOrDefault);
        }

        public static ILiveValue<T> Last<T>(this ILiveList<T> source)
        {
            return source.Aggregate(Enumerable.Last);
        }

        public static ILiveValue<T> LastOrDefault<T>(this ILiveList<T> source)
        {
            return source.Aggregate(Enumerable.LastOrDefault);
        }
    }
}
