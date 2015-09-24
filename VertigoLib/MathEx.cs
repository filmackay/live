using System;

namespace Vertigo
{
    public static class MathEx
    {
        public static decimal? Max(decimal? a, decimal? b)
        {
            if (!a.HasValue)
                return b;
            if (!b.HasValue)
                return a;
            return a.Value > b.Value ? a : b;
        }

        public static decimal? Min(decimal? a, decimal? b)
        {
            if (!a.HasValue)
                return b;
            if (!b.HasValue)
                return a;
            return a.Value < b.Value ? a : b;
        }

        public static Decimal? Round(Decimal? d, int decimals)
        {
            return d.HasValue ? Math.Round(d.Value, decimals) : (decimal?)null;
        }
    }
}
