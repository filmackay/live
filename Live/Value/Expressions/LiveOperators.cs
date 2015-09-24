using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace Vertigo.Live
{
    internal class LiveOperator<T>
    {
        public static readonly Lazy<LiveFunc<T, T, T>> _add = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Add).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _subtract = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Subtract).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _divide = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Divide).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _multiply = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Multiply).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _modulo = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Modulo).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _or = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.Or).Create());
        public static readonly Lazy<LiveFunc<T, T, T>> _and = new Lazy<LiveFunc<T, T, T>>(() => ExpressionUtil.CreateExpression<T, T, T>(Expression.And).Create());
        public static readonly Lazy<LiveFunc<T, T, bool>> _greaterThan = new Lazy<LiveFunc<T, T, bool>>(() => ExpressionUtil.CreateExpression<T, T, bool>(Expression.GreaterThan).Create());
        public static readonly Lazy<LiveFunc<T, T, bool>> _greaterThanOrEqual = new Lazy<LiveFunc<T, T, bool>>(() => ExpressionUtil.CreateExpression<T, T, bool>(Expression.GreaterThanOrEqual).Create());
        public static readonly Lazy<LiveFunc<T, T, bool>> _lessThan = new Lazy<LiveFunc<T, T, bool>>(() => ExpressionUtil.CreateExpression<T, T, bool>(Expression.LessThan).Create());
        public static readonly Lazy<LiveFunc<T, T, bool>> _lessThanOrEqual = new Lazy<LiveFunc<T, T, bool>>(() => ExpressionUtil.CreateExpression<T, T, bool>(Expression.LessThanOrEqual).Create());
        public static readonly Lazy<LiveFunc<T, T>> _not = new Lazy<LiveFunc<T, T>>(() => LiveFunc.Create(ExpressionUtil.CreateExpression<T, T>(Expression.Not)));
        public static readonly Lazy<LiveFunc<T, T>> _negate = new Lazy<LiveFunc<T, T>>(() => LiveFunc.Create(ExpressionUtil.CreateExpression<T, T>(Expression.Negate)));
        public static readonly Lazy<LiveFunc<T, T, T>> _coalesce = new Lazy<LiveFunc<T, T, T>>(() => LiveFunc.Create(ExpressionUtil.CreateExpression<T, T, T>(Expression.Coalesce)));
        public static readonly Lazy<LiveFunc<T, T, bool>> _equal = new Lazy<LiveFunc<T, T, bool>>(() => LiveFunc.Create(ExpressionUtil.CreateExpression<T, T, bool>(Expression.Equal)));
        public static readonly Lazy<LiveFunc<T, T, bool>> _notEqual = new Lazy<LiveFunc<T, T, bool>>(() => LiveFunc.Create(ExpressionUtil.CreateExpression<T, T, bool>(Expression.NotEqual)));
    }

    internal class LiveOperatorNullable<T>
        where T : struct
    {
        public static readonly Lazy<LiveFunc<T?, T?, T?>> _add = new Lazy<LiveFunc<T?, T?, T?>>(() => ExpressionUtil.CreateExpression<T?, T?, T?>(Expression.Add).Create());
    }

    public static partial class Extensions
    {
        public static ILiveValue<T> Add<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._add.Value(left, right); }

        public static ILiveValue<T?> Add<T>(this ILiveValue<T?> left, ILiveValue<T?> right)
            where T : struct
        { return LiveOperatorNullable<T>._add.Value(left, right); }

        public static ILiveValue<T> Subtract<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._subtract.Value(left, right); }

        public static ILiveValue<T> Divide<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._divide.Value(left, right); }

        public static ILiveValue<T> Multiply<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._multiply.Value(left, right); }

        public static ILiveValue<T> Modulo<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._modulo.Value(left, right); }

        public static ILiveValue<T> Or<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._or.Value(left, right); }

        public static ILiveValue<T> And<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._and.Value(left, right); }

        public static ILiveValue<bool> GreaterThan<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._greaterThan.Value(left, right); }

        public static ILiveValue<bool> GreaterThanOrEqual<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._greaterThanOrEqual.Value(left, right); }

        public static ILiveValue<bool> LessThan<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._lessThan.Value(left, right); }

        public static ILiveValue<bool> LessThanOrEqual<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._lessThanOrEqual.Value(left, right); }

        public static ILiveValue<T> Not<T>(this ILiveValue<T> left)
        { return LiveOperator<T>._not.Value(left); }

        public static ILiveValue<T> Negate<T>(this ILiveValue<T> left)
        { return LiveOperator<T>._negate.Value(left); }

        public static ILiveValue<T> IfNull<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return LiveOperator<T>._coalesce.Value(left, right); }

        public static ILiveValue<T> If<T>(this ILiveValue<bool> condition, ILiveValue<T> trueValue, ILiveValue<T> falseValue)
        { return condition.Select(b => b ? trueValue : falseValue); }

        public static ILiveValue<T> If<T>(this ILiveValue<bool> condition, T trueValue, T falseValue)
        { return condition.SelectStatic(b => b ? trueValue : falseValue); }

        public static ILiveValue<bool> Equal<T>(this ILiveValue<T> left, ILiveValue<T> right)
        { return left.Join(right, EqualityComparer<T>.Default.Equals); }

        public static ILiveValue<bool> Equal<T>(this ILiveValue<T> left, T right)
        { return left.SelectStatic(l => EqualityComparer<T>.Default.Equals(l, right)); }

        public static ILiveValue<bool> AndAlso(this ILiveValue<bool> left, ILiveValue<bool> right)
        { return left.Select(l => !l ? false.ToLiveConst() : right); }

        public static ILiveValue<bool> OrElse(this ILiveValue<bool> left, ILiveValue<bool> right)
        { return left.Select(l => l ? true.ToLiveConst() : right); }

        public static ILiveValue<T> GetValueOrDefault<T>(this ILiveValue<T?> left, T defaultValue = default(T))
            where T : struct
        {
            return left.SelectStatic(l => l.GetValueOrDefault(defaultValue));
        }

        public static ILiveValue<bool> HasValue<T>(this ILiveValue<T?> left)
            where T : struct
        {
            return left.SelectStatic(v => v.HasValue);
        }

        public static ILiveValue<TResult> Switch<TSource, TResult>(this ILiveValue<TSource> source, Tuple<TSource, ILiveValue<TResult>>[] cases, ILiveValue<TResult> defaultCase = null)
        {
            return source
                .SwitchStatic(cases)
                .Unwrap();
        }

        public static ILiveValue<TResult> SwitchStatic<TSource, TResult>(this ILiveValue<TSource> source, Tuple<TSource, TResult>[] cases, TResult defaultCase = default(TResult))
        {
            return source.SelectStatic(s =>
                {
                    for (var i = 0; i < cases.Length; i++)
                    {
                        if (cases[i].Item1.Equals(s))
                            return cases[i].Item2;
                    }
                    return defaultCase;
                });
        }

        private static readonly Lazy<LiveFunc<DateTimeOffset, DateTimeOffset, TimeSpan>> _subtractDateTimeOffset = new Lazy<LiveFunc<DateTimeOffset, DateTimeOffset, TimeSpan>>(() => ExpressionUtil.CreateExpression<DateTimeOffset, DateTimeOffset, TimeSpan>(Expression.Subtract).Create());
        public static ILiveValue<TimeSpan> Subtract(this ILiveValue<DateTimeOffset> left, ILiveValue<DateTimeOffset> right)
        { return _subtractDateTimeOffset.Value(left, right); }

        private static readonly Lazy<LiveFunc<DateTime, DateTime, TimeSpan>> _subtractDateTime = new Lazy<LiveFunc<DateTime, DateTime, TimeSpan>>(() => ExpressionUtil.CreateExpression<DateTime, DateTime, TimeSpan>(Expression.Subtract).Create());
        public static ILiveValue<TimeSpan> Subtract(this ILiveValue<DateTime> left, ILiveValue<DateTime> right)
        { return _subtractDateTime.Value(left, right); }

        public static ILiveValue<TResult> Cast<TSource,TResult>(this ILiveValue<TSource> source)
        {
            return source.SelectStatic(item => (TResult)(object)item);
        }

        public static ILiveValue<TResult> Convert<TSource,TResult>(this ILiveValue<TSource> source)
        {
            return source.SelectStatic(item => (TResult)System.Convert.ChangeType(item, typeof(TResult)));
        }
    }
}
