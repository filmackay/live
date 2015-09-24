using System;
using System.Reactive;

namespace Vertigo
{
    public static class TraceTargets
    {
        public static IObserver<string> Debug = Observer.Create<string>(i => System.Diagnostics.Debug.WriteLine(i));
        public static IObserver<string> Trace = Observer.Create<string>(i => System.Diagnostics.Trace.WriteLine(i));
    }
}