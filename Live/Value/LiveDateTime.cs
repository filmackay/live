using System;


namespace Vertigo.Live
{
    public static class LiveDateTime
    {
        public static ILiveValue<DateTime> Now()
        {
            return LiveValueObservable<DateTime>.Create(
                innerChanged => { },
                () => { },
                (innerChanged, oldState) =>
                {
                    innerChanged();
                    return new ValueState<DateTime>
                    {
                        NewValue = DateTime.Now,
                        LastUpdated = HiResTimer.Now(),
                        Status = StateStatus.Connected,
                    };
                },
                () => { });
        }

        public static ILiveValue<DateTimeOffset> NowOffset()
        {
            return LiveValueObservable<DateTimeOffset>.Create(
                innerChanged => { },
                () => { },
                (innerChanged, oldState) =>
                {
                    innerChanged();
                    return new ValueState<DateTimeOffset>
                    {
                        NewValue = DateTimeOffset.Now,
                        LastUpdated = HiResTimer.Now(),
                        Status = StateStatus.Connected,
                    };
                },
                () => { });
        }
    }
}