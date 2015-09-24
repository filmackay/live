using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveCollection<object> ToUntyped<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : class, ICollectionDelta<T>
        {
            return source.Cast<T, TIDelta, object>();
        }
    }
}
