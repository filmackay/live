using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class LiveWindow<T> : LiveValue<Tuple<TimeSpan,T>[]>
    {
        private readonly ISubscription<IValueState<T>> _subscription;
        private readonly long _windowSize;
        private readonly List<Tuple<long, double>> _window = new List<Tuple<long, double>>();

        public LiveWindow(ILiveValue<T> source, TimeSpan windowTimeSpan)
        {
            _windowSize = HiResTimer.FromTimeSpan(windowTimeSpan);
            _subscription = source.Subscribe();
            _subscription.ConnectAndNotify(InnerChanged);
        }

        public override void InnerGetValue(ref double value, ref StateStatus startType, ref long lastUpdated, IDisposable sourceLock)
        {
            // get source change
            var state = _subscription.GetState();

            // create new change
            if (state.Status.IsConnected())
            {
                var now = HiResTimer.Now;

                // clip window - but always leave at least one item
                var removeItems = 0;
                while (removeItems < (_window.Count - 1) && _window[removeItems].Item1 < (now - _windowSize))
                    removeItems++;
                _window.RemoveRange(0, removeItems);

                // add to window
                _window.Add(new Tuple<long, double>(now, state.NewValue));

                // enough data?
                startType = startType.AddSimple(_window.Count < 2 ? StateStatus.Disconnected : StateStatus.Connected);

                if (startType != StateStatus.Disconnected)
                {
                    var ratesPassed = ((double)now - _window[0].Item1) / _unitSize;
                    value = ratesPassed == 0 ? 0 : (state.NewValue - _window[0].Item2) / ratesPassed;
                    lastUpdated = state.LastUpdated;
                }
            }
            else
            {
                startType = startType.AddSimple(state.Status);
                _window.Clear();
            }
        }

        public override void Dispose()
        {
            _subscription.Complete();
        }
    }

    public static partial class Extensions
    {
        public static LiveValue<double> ToLiveRateOfChange(this LiveValue<double> source, TimeSpan rateTimeSpan, TimeSpan windowTimeSpan)
        {
            return new LiveRateOfChange(source, rateTimeSpan, windowTimeSpan);
        }
    }
}
