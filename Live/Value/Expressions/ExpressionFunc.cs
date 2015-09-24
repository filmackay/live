using System;
using System.Linq.Expressions;

namespace Vertigo.Live
{
    public static class ExpressionFunc<T>
    {
        public static Func<T, T> Negate = Create(Expression.Negate);
        public static Func<T, T, T> Add = Create<T>(Expression.Add);
        public static Func<T, T, T> Subtract = Create<T>(Expression.Subtract);
        public static Func<T, T, T> Multiply = Create<T>(Expression.Multiply);
        public static Func<T, T, T> Divide = Create<T>(Expression.Divide);
        public static Func<T, T, T> Modulo = Create<T>(Expression.Modulo);
        public static Func<T, T, bool> GreaterThan = Create<bool>(Expression.GreaterThan);
        public static Func<T, T, bool> GreaterThanOrEqual = Create<bool>(Expression.GreaterThanOrEqual);
        public static Func<T, T, bool> LessThan = Create<bool>(Expression.LessThan);
        public static Func<T, T, bool> LessThanOrEqual = Create<bool>(Expression.LessThanOrEqual);

        public static Func<T, T, TResult> Create<TResult>(Func<Expression, Expression, BinaryExpression> op)
        {
            // declare the parameters
            var paramA = Expression.Parameter(typeof(T), "a");
            var paramB = Expression.Parameter(typeof(T), "b");

            // add the parameters together
            var body = op(paramA, paramB);

            // compile it
            return Expression
                .Lambda<Func<T, T, TResult>>(body, paramA, paramB)
                .Compile();
        }

        public static Func<T, T> Create(Func<Expression, UnaryExpression> op)
        {
            // declare the parameters
            var paramA = Expression.Parameter(typeof(T), "a");
            // add the parameters together
            var body = op(paramA);
            // compile it
            return Expression.Lambda<Func<T, T>>(body, paramA).Compile();
        }
    }

    public static class ExpressionFuncInt<T>
    {
        public static Func<T, T, T> And = ExpressionFunc<T>.Create<T>(Expression.And);
        public static Func<T, T, T> Xor = ExpressionFunc<T>.Create<T>(Expression.ExclusiveOr);
    }

    public static class ExpressionFuncBool
    {
        public static Func<bool, bool, bool> AndAlso = ExpressionFunc<bool>.Create<bool>(Expression.AndAlso);
        public static Func<bool, bool, bool> OrElse = ExpressionFunc<bool>.Create<bool>(Expression.OrElse);
    }
}
