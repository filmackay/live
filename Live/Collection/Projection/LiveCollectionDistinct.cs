using System;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveSet<T> Distinct<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : class, ICollectionDelta<T>
        {
            return source
                .SelectStatic(i => KeyValuePair.Create(i, i))
                .Group()
                .Keys();
        }
    }
}
