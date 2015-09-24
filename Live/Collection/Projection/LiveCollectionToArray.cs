using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveValue<T[]> ToLiveArray<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            LiveObserver<ICollectionState<T, TIDelta>> observer = null;

            return LiveValueObservable<T[]>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    using (var state = observer.GetState())
                    {
                        return oldState.Add(
                            new ValueState<T[]>
                            {
                                LastUpdated = state.LastUpdated,
                                NewValue = state.Inner.ToArray(),
                                Status = state.Status,
                            });
                    }

                },
                () => observer.Dispose());
        }

        public static Task<T[]> ToArrayAsync<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .States()
                .Select(state => state.Inner == null ? null : state.Inner.ToArray())
                .FirstAsync()
                .ToTask();
        }

        public static T[] ToArray<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source.ToArrayAsync().Result;
        }
    }
}