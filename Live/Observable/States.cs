using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Vertigo.Live
{
    public static partial class Extensions
    {
        public static IObservable<TIState> AllStates<TIState>(this ILiveObservable<TIState> source)
            where TIState : IState
        {
            if (source == null)
                Debug.Assert(source != null);

            return Observable.Create<TIState>(observer =>
                {
                    // subscribe to source
                    var isFirst = true;
                    return source.Subscribe(
                        source.CreateObserver(lo =>
                        {
                            if (isFirst)
                            {
                                // initial state
                                lo.UseState(observer.OnNext);
                                isFirst = false;
                            }
                            else
                            {
                                Publish.OnConsume(
                                    () =>
                                    lo.UseState(
                                        state =>
                                        {
                                            observer.OnNext(state);
                                            if (state.Status == StateStatus.Completing)
                                                observer.OnCompleted();
                                        })
                                    );
                            }
                        }));
                });
        }

        public static IObservable<TIState> States<TIState>(this ILiveObservable<TIState> source)
            where TIState : IState
        {
            return
                source
                    .AllStates()
                    .Where(state => state.HasEffect());
        }
    }
}
