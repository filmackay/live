using System;

namespace Vertigo.Live
{
    public class LiveStats
    {
        public static ILiveValue<double> Correlation(ILiveList<double> array1, ILiveList<double> array2)
        {
            var avg1 = array1.Average();
            var avg2 = array2.Average();

            var diff1 = array1.Select(val1 => val1.ToLiveConst().Subtract(avg1));
            var diff2 = array2.Select(val2 => val2.ToLiveConst().Subtract(avg2));

           // var num6 = diff1.Zip(diff2).SelectStatic(t => t.Item1 * t.Item2).Sum();
            var num7 = diff1.SelectStatic(val1 => val1 * val1).Sum();
            var num8 = diff2.SelectStatic(val2 => val2 * val2).Sum();

            //return (num6.Divide(LiveMath.Sqrt(num7.Multiply(num8))));
            return null;
        }
    }
}
