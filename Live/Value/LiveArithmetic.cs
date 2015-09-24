using System;
using System.Linq;

namespace Vertigo.Live
{
    public static partial class Extensions
	{
		#region Sum() overloads

		// int
		public static ILiveValue<int> Sum<TIDelta>(this ILiveCollection<int, TIDelta> source)
            where TIDelta : ICollectionDelta<int>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<int> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<int> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// int?
		public static ILiveValue<int?> Sum<TIDelta>(this ILiveCollection<int?, TIDelta> source)
            where TIDelta : ICollectionDelta<int?>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<int?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<int?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// long
		public static ILiveValue<long> Sum<TIDelta>(this ILiveCollection<long, TIDelta> source)
            where TIDelta : ICollectionDelta<long>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<long> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<long> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// long?
		public static ILiveValue<long?> Sum<TIDelta>(this ILiveCollection<long?, TIDelta> source)
            where TIDelta : ICollectionDelta<long?>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<long?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<long?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// decimal
		public static ILiveValue<decimal> Sum<TIDelta>(this ILiveCollection<decimal, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<decimal> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<decimal> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// decimal?
		public static ILiveValue<decimal?> Sum<TIDelta>(this ILiveCollection<decimal?, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal?>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<decimal?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<decimal?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// float
		public static ILiveValue<float> Sum<TIDelta>(this ILiveCollection<float, TIDelta> source)
            where TIDelta : ICollectionDelta<float>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<float> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<float> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// float?
		public static ILiveValue<float?> Sum<TIDelta>(this ILiveCollection<float?, TIDelta> source)
            where TIDelta : ICollectionDelta<float?>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<float?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<float?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// double
		public static ILiveValue<double> Sum<TIDelta>(this ILiveCollection<double, TIDelta> source)
            where TIDelta : ICollectionDelta<double>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<double> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<double> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		// double?
		public static ILiveValue<double?> Sum<TIDelta>(this ILiveCollection<double?, TIDelta> source)
            where TIDelta : ICollectionDelta<double?>
        {
			return source.Aggregate(items => items.Sum(), (agg, state) => agg + state.Delta.Inserts.Sum() - state.Delta.Deletes.Sum());
        }

		public static ILiveValue<double?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Sum();
        }

		public static ILiveValue<double?> Sum<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Sum();
        }

		#endregion

		#region Min() overloads

		// int
		public static ILiveValue<int> Min<TIDelta>(this ILiveCollection<int, TIDelta> source)
            where TIDelta : ICollectionDelta<int>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<int> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<int> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// int?
		public static ILiveValue<int?> Min<TIDelta>(this ILiveCollection<int?, TIDelta> source)
            where TIDelta : ICollectionDelta<int?>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg || agg == null)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<int?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<int?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// long
		public static ILiveValue<long> Min<TIDelta>(this ILiveCollection<long, TIDelta> source)
            where TIDelta : ICollectionDelta<long>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<long> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<long> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// long?
		public static ILiveValue<long?> Min<TIDelta>(this ILiveCollection<long?, TIDelta> source)
            where TIDelta : ICollectionDelta<long?>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg || agg == null)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<long?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<long?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// decimal
		public static ILiveValue<decimal> Min<TIDelta>(this ILiveCollection<decimal, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<decimal> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<decimal> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// decimal?
		public static ILiveValue<decimal?> Min<TIDelta>(this ILiveCollection<decimal?, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal?>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg || agg == null)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<decimal?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<decimal?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// float
		public static ILiveValue<float> Min<TIDelta>(this ILiveCollection<float, TIDelta> source)
            where TIDelta : ICollectionDelta<float>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<float> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<float> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// float?
		public static ILiveValue<float?> Min<TIDelta>(this ILiveCollection<float?, TIDelta> source)
            where TIDelta : ICollectionDelta<float?>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg || agg == null)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<float?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<float?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// double
		public static ILiveValue<double> Min<TIDelta>(this ILiveCollection<double, TIDelta> source)
            where TIDelta : ICollectionDelta<double>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<double> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<double> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		// double?
		public static ILiveValue<double?> Min<TIDelta>(this ILiveCollection<double?, TIDelta> source)
            where TIDelta : ICollectionDelta<double?>
        {
			return source.Aggregate(
		        Enumerable.Min,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
			                var minInsert = state.Delta.Inserts.Min();
			                if (minInsert < agg || agg == null)
			                    return minInsert;
						}
                        if (state.Delta.Deletes != null)
                        {
			                if (state.Delta.Deletes.Min() == agg)
			                    return state.Inner.Min();
						}
			            return agg;
		            }
				);
	        }

		public static ILiveValue<double?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Min();
        }

		public static ILiveValue<double?> Min<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Min();
        }

		#endregion

		#region Max() overloads

		// int
		public static ILiveValue<int> Max<TIDelta>(this ILiveCollection<int, TIDelta> source)
            where TIDelta : ICollectionDelta<int>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<int> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<int> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// int?
		public static ILiveValue<int?> Max<TIDelta>(this ILiveCollection<int?, TIDelta> source)
            where TIDelta : ICollectionDelta<int?>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg || agg == null)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<int?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<int?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// long
		public static ILiveValue<long> Max<TIDelta>(this ILiveCollection<long, TIDelta> source)
            where TIDelta : ICollectionDelta<long>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<long> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<long> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// long?
		public static ILiveValue<long?> Max<TIDelta>(this ILiveCollection<long?, TIDelta> source)
            where TIDelta : ICollectionDelta<long?>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg || agg == null)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<long?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<long?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// decimal
		public static ILiveValue<decimal> Max<TIDelta>(this ILiveCollection<decimal, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<decimal> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<decimal> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// decimal?
		public static ILiveValue<decimal?> Max<TIDelta>(this ILiveCollection<decimal?, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal?>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg || agg == null)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<decimal?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<decimal?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// float
		public static ILiveValue<float> Max<TIDelta>(this ILiveCollection<float, TIDelta> source)
            where TIDelta : ICollectionDelta<float>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<float> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<float> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// float?
		public static ILiveValue<float?> Max<TIDelta>(this ILiveCollection<float?, TIDelta> source)
            where TIDelta : ICollectionDelta<float?>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg || agg == null)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<float?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<float?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// double
		public static ILiveValue<double> Max<TIDelta>(this ILiveCollection<double, TIDelta> source)
            where TIDelta : ICollectionDelta<double>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<double> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<double> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		// double?
		public static ILiveValue<double?> Max<TIDelta>(this ILiveCollection<double?, TIDelta> source)
            where TIDelta : ICollectionDelta<double?>
        {
			return source.Aggregate(
		        Enumerable.Max,
		        (agg, state) =>
		            {
                        if (state.Delta.Inserts != null)
                        {
                            var maxInsert = state.Delta.Inserts.Max();
                            if (maxInsert > agg || agg == null)
                                return maxInsert;
                        }
                        if (state.Delta.Deletes != null)
                        {
                            if (state.Delta.Deletes.Max() == agg)
                                return state.Inner.Max();
                        }
			            return agg;
		            }
				);
	        }

		public static ILiveValue<double?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Max();
        }

		public static ILiveValue<double?> Max<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Max();
        }

		#endregion

		#region Average() overloads

		// int
		public static ILiveValue<double> Average<TIDelta>(this ILiveCollection<int, TIDelta> source)
            where TIDelta : ICollectionDelta<int>
        {
			return source
				.Sum()
				.Convert<int,double>()
				.Divide(source.Count()
					.Convert<int,double>()
				);
        }

		public static ILiveValue<double> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<double> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// int?
		public static ILiveValue<int?> Average<TIDelta>(this ILiveCollection<int?, TIDelta> source)
            where TIDelta : ICollectionDelta<int?>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,int?>()
				);
        }

		public static ILiveValue<int?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<int?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<int?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, int?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// long
		public static ILiveValue<long> Average<TIDelta>(this ILiveCollection<long, TIDelta> source)
            where TIDelta : ICollectionDelta<long>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,long>()
				);
        }

		public static ILiveValue<long> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<long> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// long?
		public static ILiveValue<long?> Average<TIDelta>(this ILiveCollection<long?, TIDelta> source)
            where TIDelta : ICollectionDelta<long?>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,long?>()
				);
        }

		public static ILiveValue<long?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<long?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<long?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, long?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// decimal
		public static ILiveValue<decimal> Average<TIDelta>(this ILiveCollection<decimal, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,decimal>()
				);
        }

		public static ILiveValue<decimal> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<decimal> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// decimal?
		public static ILiveValue<decimal?> Average<TIDelta>(this ILiveCollection<decimal?, TIDelta> source)
            where TIDelta : ICollectionDelta<decimal?>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,decimal?>()
				);
        }

		public static ILiveValue<decimal?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<decimal?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<decimal?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, decimal?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// float
		public static ILiveValue<float> Average<TIDelta>(this ILiveCollection<float, TIDelta> source)
            where TIDelta : ICollectionDelta<float>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,float>()
				);
        }

		public static ILiveValue<float> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<float> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// float?
		public static ILiveValue<float?> Average<TIDelta>(this ILiveCollection<float?, TIDelta> source)
            where TIDelta : ICollectionDelta<float?>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,float?>()
				);
        }

		public static ILiveValue<float?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<float?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<float?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, float?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// double
		public static ILiveValue<double> Average<TIDelta>(this ILiveCollection<double, TIDelta> source)
            where TIDelta : ICollectionDelta<double>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,double>()
				);
        }

		public static ILiveValue<double> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<double> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		// double?
		public static ILiveValue<double?> Average<TIDelta>(this ILiveCollection<double?, TIDelta> source)
            where TIDelta : ICollectionDelta<double?>
        {
			return source
				.Sum()
				.Divide(source.Count()
					.Convert<int,double?>()
				);
        }

		public static ILiveValue<double?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, ILiveValue<double?>> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.Select(selector)
				.Average();
        }

		public static ILiveValue<double?> Average<T, TIDelta>(this ILiveCollection<T, TIDelta> source, Func<T, double?> selector)
            where TIDelta : ICollectionDelta<T>
        {
			return source
				.SelectStatic(selector)
				.Average();
        }

		#endregion

	}
}

