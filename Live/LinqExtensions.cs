using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    // generic versions of Max/Min where you can provide a comparer
    public static class LinqExtensions
    {
        public static TSource Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var y = default(TSource);
            if (y == null)
            {
                foreach (var local2 in source)
                {
                    if ((local2 != null) && ((y == null) || (comparer.Compare(local2, y) > 0)))
                    {
                        y = local2;
                    }
                }
                return y;
            }
            var flag = false;
            foreach (var local3 in source)
            {
                if (flag)
                {
                    if (comparer.Compare(local3, y) > 0)
                    {
                        y = local3;
                    }
                }
                else
                {
                    y = local3;
                    flag = true;
                }
            }

            if (!flag)
                throw new ArgumentException("No elements");
            return y;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var y = default(TSource);
            if (y == null)
            {
                foreach (var local2 in source)
                {
                    if ((local2 != null) && ((y == null) || (comparer.Compare(local2, y) < 0)))
                    {
                        y = local2;
                    }
                }
                return y;
            }
            var flag = false;
            foreach (var local3 in source)
            {
                if (flag)
                {
                    if (comparer.Compare(local3, y) < 0)
                    {
                        y = local3;
                    }
                }
                else
                {
                    y = local3;
                    flag = true;
                }
            }
            if (!flag)
                throw new ArgumentException("No elements");
            return y;
        }
    }
}
