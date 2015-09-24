using System;
using Vertigo.Live;

namespace Vertigo
{
    public static partial class Extensions
    {
        public static ILiveValue<decimal> Normalize(this ILiveValue<decimal> value)
        {
            return value.SelectStatic(d => d.Normalize());
        }
        public static ILiveValue<decimal?> Normalize(this ILiveValue<decimal?> value)
        {
            return value.SelectStatic(d => d.Normalize());
        }
    }
}
