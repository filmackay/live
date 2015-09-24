using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static ILiveSet<object> ToUntyped<T>(this ILiveSet<T> source)
        {
            return source.Cast<T, object>();
        }
    }
}
