using System;

namespace Vertigo.Live
{
    public interface ILiveObservable<out TIState>
        where TIState : IState
    {
        IDisposable Subscribe(ILiveObserver<TIState> observer);
    }

    public static partial class LiveObservable
    {
        public static LiveObserver<TIState> Subscribe<TIState>(this ILiveObservable<TIState> observable, Action<LiveObserver<TIState>> onNotify)
            where TIState : IState
        {
            LiveObserver<TIState> observer;
            observable.Subscribe(observer = observable.CreateObserver(onNotify));
            return observer;
        }
    }
}
