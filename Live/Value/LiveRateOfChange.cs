using System;
using System.Collections.Generic;


namespace Vertigo.Live
{
    public static partial class LiveValue
    {
        public static ILiveValue<double> ToLiveRateOfChange(this ILiveValue<double> source, TimeSpan unitTimeSpan, TimeSpan windowTimeSpan)
        {
            var unitSize = HiResTimer.FromTimeSpan(unitTimeSpan);
            var windowSize = HiResTimer.FromTimeSpan(windowTimeSpan);
            var window = new List<Tuple<long, double>>();
            LiveObserver<IValueState<double>> observer = null;

            return LiveValueObservable<double>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    // get source change
                    var state = observer.GetState();

                    // create new change
                    var result = new ValueState<double>();
                    if (state.Status.IsConnected())
                    {
                        var now = HiResTimer.Now();

                        // clip window - but always leave at least one item
                        var removeItems = 0;
                        while (removeItems < (window.Count - 1) && window[removeItems].Item1 < (now - windowSize))
                            removeItems++;
                        window.RemoveRange(0, removeItems);

                        // add to window
                        window.Add(new Tuple<long, double>(now, state.NewValue));

                        // enough data?
                        result.Status = window.Count < 2 ? StateStatus.Disconnected : StateStatus.Connected;
                        if (result.Status != StateStatus.Disconnected)
                        {
                            var ratesPassed = ((double)now - window[0].Item1) / unitSize;
                            result.NewValue = ratesPassed == 0 ? 0 : (state.NewValue - window[0].Item2) / ratesPassed;
                            result.LastUpdated = state.LastUpdated;
                        }
                    }
                    else
                    {
                        result.Status = state.Status;
                        window.Clear();
                    }
                    return result;
                },
                () => observer.Dispose());
        }

        public static ILiveValue<double> ToLiveRateOfChange(this ILiveValue<double> source)
        {
            return source.ToLiveRateOfChange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }
}
