using System;

namespace Vertigo.Live
{
    public static class LiveMath
    {
        public static LiveFunc<double, double, double> Pow = ((Func<double, double, double>)Math.Pow).Create();
        public static LiveFunc<double, double> Abs = LiveFunc.Create((Func<double, double>)Math.Abs);

        private static LiveFunc<double, int, MidpointRounding, double> _roundDouble = ((Func<double, int, MidpointRounding, double>)Math.Round).Create();
        public static ILiveValue<double> Round(ILiveValue<double> value, ILiveValue<int> digits = null, ILiveValue<MidpointRounding> mode = null)
        {
            return _roundDouble(value, digits ?? 0.ToLiveConst(), mode ?? MidpointRounding.AwayFromZero.ToLiveConst());
        }

        private static LiveFunc<double?, int, MidpointRounding, double?> _roundDoubleNullable = new Func<double?, int, MidpointRounding, double?>((v, d, m) => !v.HasValue ? (double?)null : Math.Round(v.Value, d, m)).Create();
        public static ILiveValue<double?> Round(ILiveValue<double?> value, ILiveValue<int> digits = null, ILiveValue<MidpointRounding> mode = null)
        {
            return _roundDoubleNullable(value, digits ?? 0.ToLiveConst(), mode ?? MidpointRounding.AwayFromZero.ToLiveConst());
        }

        private static LiveFunc<decimal, int, MidpointRounding, decimal> _roundDecimal = ((Func<decimal, int, MidpointRounding, decimal>)Math.Round).Create();
        public static ILiveValue<decimal> Round(ILiveValue<decimal> value, ILiveValue<int> digits = null, ILiveValue<MidpointRounding> mode = null)
        {
            return _roundDecimal(value, digits ?? 0.ToLiveConst(), mode ?? MidpointRounding.AwayFromZero.ToLiveConst());
        }

        private static LiveFunc<decimal?, int, MidpointRounding, decimal?> _roundDecimalNullable = new Func<decimal?, int, MidpointRounding, decimal?>((v, d, m) => !v.HasValue ? (decimal?)null : Math.Round(v.Value, d, m)).Create();
        public static ILiveValue<decimal?> Round(ILiveValue<decimal?> value, ILiveValue<int> digits = null, ILiveValue<MidpointRounding> mode = null)
        {
            return _roundDecimalNullable(value, digits ?? 0.ToLiveConst(), mode ?? MidpointRounding.AwayFromZero.ToLiveConst());
        }

        public static LiveFunc<double, double> Sqrt = LiveFunc.Create<double, double>(Math.Sqrt);

        private static LiveFunc<decimal, decimal, decimal> _maxDecimal = ((Func<decimal, decimal, decimal>)Math.Max).Create();
        public static ILiveValue<decimal> Max(ILiveValue<decimal> a, ILiveValue<decimal> b)
        {
            return _maxDecimal(a, b);
        }

        private static LiveFunc<decimal, decimal, decimal> _minDecimal = ((Func<decimal, decimal, decimal>)Math.Min).Create();
        public static ILiveValue<decimal> Min(ILiveValue<decimal> a, ILiveValue<decimal> b)
        {
            return _minDecimal(a, b);
        }

        private static LiveFunc<decimal?, decimal?, decimal?> _minDecimalNullable = new Func<decimal?, decimal?, decimal?>((a, b) => !a.HasValue ? (decimal?)null : !b.HasValue ? (decimal?)null : Math.Min(a.Value, b.Value)).Create();
        public static ILiveValue<decimal?> Min(ILiveValue<decimal?> a, ILiveValue<decimal?> b)
        {
            return _minDecimalNullable(a, b);
        }

        private static LiveFunc<decimal, decimal> _floorDecimal = LiveFunc.Create((Func<decimal, decimal>)Math.Floor);
        public static ILiveValue<decimal> Floor(ILiveValue<decimal> a)
        {
            return _floorDecimal(a);
        }

        private static LiveFunc<double, double> _floorDouble = LiveFunc.Create((Func<double, double>)Math.Floor);
        public static ILiveValue<double> Floor(ILiveValue<double> a)
        {
            return _floorDouble(a);
        }

        private static LiveFunc<double?, double?> _floorDoubleNullable = LiveFunc.Create<double?, double?>(a => !a.HasValue ? (double?)null : Math.Floor(a.Value));
        public static ILiveValue<double?> Floor(ILiveValue<double?> a)
        {
            return _floorDoubleNullable(a);
        }
    }
}