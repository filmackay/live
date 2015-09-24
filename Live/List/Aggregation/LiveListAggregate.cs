using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public static partial class LiveList
    {
        public static ILiveValue<TResult> Aggregate<TSource, TResult>(this ILiveList<TSource> source, Func<TResult, ICollectionState<TSource, IListDelta<TSource>>, TResult> applyState, Func<TResult, IListDelta<TSource>, TResult> postApplyState = null)
        {
            LiveObserver<ICollectionState<TSource, IListDelta<TSource>>> observer = null;
            var aggregate = default(TResult);

            return LiveValueObservable<TResult>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    ICollectionState<TSource, IListDelta<TSource>> state;

                    using (state = observer.GetState())
                    {
                        if (state.HasEffect())
                            aggregate = applyState(aggregate, state);
                    }

                    // call without source to reduce locking
                    if (state.Delta.HasChange() && postApplyState != null)
                        aggregate = postApplyState(aggregate, state.Delta);

                    return new ValueState<TResult>
                    {
                        Status = state.Status,
                        NewValue = aggregate,
                        LastUpdated = state.LastUpdated,
                    };
                },
                () => observer.Dispose());
        }

        public static ILiveValue<TResult> Aggregate<TSource, TResult>(this ILiveList<TSource> source, Func<IEnumerable<TSource>, TResult> start, Func<TResult, TSource, TResult> add, Func<TResult, TSource, TResult> remove)
        {
            if (add == null && remove == null)
                return source.Aggregate(start);
            return source.Aggregate<TSource, TResult>(
                (result, state) =>
                {
                    if (state.Status.IsConnecting())
                        return start(state.Inner);
                    return result;
                });
        }

        public static ILiveValue<TResult> Aggregate<TSource, TResult>(this ILiveList<TSource> source, Func<IEnumerable<TSource>, TResult> start)
        {
            return source.Aggregate<TSource, TResult>((result, state) => start(state.Inner));
        }
    }
}
