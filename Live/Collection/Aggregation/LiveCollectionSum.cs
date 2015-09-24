using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public class LiveCollectionSum<TInner, TICollection, TILiveCollection, TIDelta> : LiveCollectionAggregate<TInner, TICollection, TILiveCollection, TIDelta, TInner>
        where TICollection : ICollection<TInner>
        where TILiveCollection : ILiveCollection<TInner, TICollection, TILiveCollection, TIDelta>
        where TIDelta : class, ICollectionDelta<TInner, TICollection, TIDelta> 
    {
        public LiveCollectionSum(ILiveCollection<TInner, TICollection, TILiveCollection, TIDelta> innerCollection)
            : base(innerCollection)
        {
        }

        private TInner _sum;

        protected override TInner Result
        {
            get { return _sum; }
        }

        protected override void AggregateDelta(TIDelta delta)
        {
            foreach (var add in delta.Inserts)
                _sum = ExpressionFunc<TInner>.Add(_sum, add);
            foreach (var remove in delta.Deletes)
                _sum = ExpressionFunc<TInner>.Subtract(_sum, remove);
        }

        protected override void AggregateDelta(IEnumerable<TInner> source, TIDelta delta)
        {
        }

        protected override void AggregateStart(IEnumerable<TInner> source)
        {
            _sum = default(TInner);
            foreach (var item in source)
                _sum = ExpressionFunc<TInner>.Add(_sum, item);
        }
    }

    public static partial class Extensions
    {
        public static LiveValue<TResult> Sum<TSource, TResult, TICollection, TILiveCollection, TIDelta>(this ILiveCollection<TSource, TICollection, TILiveCollection, TIDelta> innerCollection, Func<TSource, TResult> selector)
            where TICollection : ICollection<TSource>
            where TILiveCollection : ILiveCollection<TSource, TICollection, TILiveCollection, TIDelta>
            where TIDelta : class, ICollectionDelta<TSource, TICollection, TIDelta> 
        {
            return innerCollection
                .Select(selector)
                .Sum();
        }

        public static LiveValue<TResult> Sum<TSource, TResult, TICollection, TILiveCollection, TIDelta>(this ILiveCollection<TSource, TICollection, TILiveCollection, TIDelta> innerCollection, Func<TSource, LiveValue<TResult>> selector)
            where TICollection : ICollection<TSource>
            where TILiveCollection : ILiveCollection<TSource, TICollection, TILiveCollection, TIDelta>
            where TIDelta : class, ICollectionDelta<TSource, TICollection, TIDelta> 
        {
            return innerCollection
                .Select(selector)
                .Values()
                .Sum();
        }

        public static LiveValue<TInner> Sum<TInner, TICollection, TILiveCollection, TIDelta>(this ILiveCollection<TInner, TICollection, TILiveCollection, TIDelta> innerCollection)
            where TICollection : ICollection<TInner>
            where TILiveCollection : ILiveCollection<TInner, TICollection, TILiveCollection, TIDelta>
            where TIDelta : class, ICollectionDelta<TInner, TICollection, TIDelta> 
        {
            return new LiveCollectionSum<TInner, TICollection, TILiveCollection, TIDelta>(innerCollection);
        }
    }
}
