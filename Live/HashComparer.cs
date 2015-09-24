using System.Collections.Generic;

namespace Vertigo.Live
{
    public class HashComparer<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            var hashX = x.GetHashCode();
            var hashY = y.GetHashCode();
            return hashX.GetHashCode().CompareTo(hashY.GetHashCode());
        }

        public static HashComparer<T> Default = new HashComparer<T>();
    }
}