using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class LiveCollection
    {
        public static ILiveValue<TResult> Aggregate<TSource, TSourceIDelta, TResult>(this ILiveCollection<TSource, TSourceIDelta> source, Func<TResult, ICollectionState<TSource, TSourceIDelta>, TResult> applyState = null)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            LiveObserver<ICollectionState<TSource, TSourceIDelta>> observer = null;
            var aggregate = default(TResult);

            return LiveValueObservable<TResult>.Create(
                innerChanged => source.Subscribe(observer = source.CreateObserver(innerChanged)),
                () => observer.GetNotify(),
                (innerChanged, oldState) =>
                {
                    var result = new ValueState<TResult>();

                    ICollectionState<TSource, TSourceIDelta> sourceState;
                    using (sourceState = observer.GetState())
                    {
                        // time critical
                        result.Status = oldState.Status.AddSimple(sourceState.Status);
                        result.NewValue = aggregate = applyState(aggregate, sourceState);
                        result.LastUpdated = sourceState.LastUpdated;
                    }
                    return result;
                },
                () => observer.Dispose())
                .DistinctUntilChanged();
        }

        public static ILiveValue<TResult> Aggregate<TSource, TSourceIDelta, TResult>(this ILiveCollection<TSource, TSourceIDelta> source, Func<IEnumerable<TSource>, TResult> start, Func<TResult, ICollectionState<TSource, TSourceIDelta>, TResult> applyDelta = null)
            where TSourceIDelta : ICollectionDelta<TSource>
        {
            return source.Aggregate<TSource, TSourceIDelta, TResult>(
                (aggregate, state) =>
                {
                    if (state.Status.IsConnecting())
                        aggregate = start(state.Inner);
                    else if (state.Delta.HasChange())
                        aggregate = applyDelta == null ? start(state.Inner) : applyDelta(aggregate, state);
                    return aggregate;
                });
        }

        public static ILiveValue<bool> Any<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Count().GreaterThan(0.ToLiveConst());
        }

        public static ILiveValue<bool> Any<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<bool>> where = null)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Where(where).Any();
        }

        public static ILiveValue<T> Single<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Aggregate(Enumerable.Single);
        }

        public static ILiveValue<T> Single<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<bool>> where = null)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Where(where).Single();
        }

        public static ILiveValue<T> SingleOrDefault<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source.DefaultIfEmpty().Single();
        }

        public static ILiveValue<T> SingleOrDefault<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<bool>> where = null)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Where(where).SingleOrDefault();
        }

        public static ILiveValue<int> Count<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return
                source.Aggregate(
                    Enumerable.Count,
                        (agg, state) =>
                            agg + (state.Delta.Inserts == null ? 0 : state.Delta.Inserts.Count()) - (state.Delta.Deletes == null ? 0 : state.Delta.Deletes.Count()));
        }

        public static ILiveValue<int> Count<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<bool>> where = null)
            where TIDelta : ICollectionDelta<T>
        {
            return source.Where(where).Count();
        }

        public static ILiveCollection<T> DefaultIfEmpty<T, TIDelta>(this ILiveCollection<T, TIDelta> source)
            where TIDelta : ICollectionDelta<T>
        {
            return source
                .Count()
                .Select(count =>
                    count == 0
                        ? LiveCollection<T>.Default
                        : source.ToLiveCollection());
        }

        public static ILiveList<T> DefaultIfEmpty<T>(this ILiveList<T> source)
        {
            return source
                .Count()
                .Select(count =>
                    count == 0
                        ? LiveList<T>.Default
                        : source);
        }
    }
}