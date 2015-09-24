using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveDictionary<TGroupKey, ILiveSet<TUniqueElement>> GroupBy<TGroupKey, TUniqueElement>(this ILiveSet<TUniqueElement> source, Func<TUniqueElement, ILiveValue<TGroupKey>> keySelector)
        {
            return source
                .SelectDictionary(keySelector)
                .Group();
        }

        public static ILiveDictionary<TGroupKey, ILiveSet<TUniqueElement>> GroupByStatic<TGroupKey, TUniqueElement>(this ILiveSet<TUniqueElement> source, Func<TUniqueElement, TGroupKey> keySelector)
        {
            return source
                .SelectDictionaryStatic(keySelector)
                .Group();
        }

        public static ILiveDictionary<TGroupKey, ILiveSet<TUniqueElement>> GroupBy<TSource, TGroupKey, TUniqueElement>(this ILiveSet<TSource> source, Func<TSource, ILiveValue<TGroupKey>> keySelector, Func<TSource, ILiveValue<TUniqueElement>> elementSelector)
        {
            return source
                .ToLiveDictionary(elementSelector, keySelector)
                .Group();
        }
    }
}
