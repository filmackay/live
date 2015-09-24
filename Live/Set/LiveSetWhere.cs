using System;

namespace Vertigo.Live
{
    public static partial class LiveSet
    {
        public static ILiveSet<T> Where<T>(this ILiveSet<T> source, Func<T, ILiveValue<bool>> filter)
        {
            return source.SelectDictionary(filter).Filter();
        }

        public static ILiveSet<T> Where<T>(this ILiveSet<T> source, Func<T, bool> filter)
        {
            return source.SelectDictionaryStatic(filter).Filter();
        }
    }
}
