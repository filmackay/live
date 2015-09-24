using System;
using System.Reactive.Disposables;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace LiveDemo
{
    public static partial class Extensions
    {
        public static double Distance(this Point p1, Point p2)
        {
            double xDist = p1.X - p2.X;
            double yDist = p1.Y - p2.Y;
            return Math.Sqrt(xDist * xDist + yDist * yDist);
        }
    }
}
