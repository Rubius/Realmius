using System;
using System.Linq;
using System.Linq.Expressions;

namespace RealmSync.Server.Expressions
{
    public static class Utility
    {

        public class ReplaceParameterVisitor<TResult> : ExpressionVisitor
        {
            private readonly ParameterExpression parameter;
            private readonly Expression replacement;

            public ReplaceParameterVisitor(ParameterExpression parameter, Expression replacement)
            {
                this.parameter = parameter;
                this.replacement = replacement;
            }

            public Expression<TResult> Visit<T>(Expression<T> node)
            {
                var parameters = node.Parameters.Where(p => p != parameter);
                return Expression.Lambda<TResult>(Visit(node.Body), parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == parameter ? replacement : base.VisitParameter(node);
            }
        }

        private static Expression<Func<TResult>> WithParametersOf<T, TResult>(this Expression<Func<T, TResult>> left, Expression<Func<T, TResult>> right)
        {
            return new ReplaceParameterVisitor<Func<TResult>>(left.Parameters[0], right.Parameters[0]).Visit(left);
        }
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, right.WithParametersOf(left).Body), left.Parameters);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, right.WithParametersOf(left).Body), left.Parameters);
        }


        //public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        //{
        //    // build parameter map (from parameters of second to parameters of first)
        //    var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

        //    // replace parameters in the second lambda expression with parameters from the first
        //    var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

        //    // apply composition of lambda expression bodies to parameters from the first expression 
        //    return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        //}

        //public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        //{
        //    return first.Compose(second, Expression.And);
        //}

        //public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        //{
        //    return first.Compose(second, Expression.Or);
        //}
    }
}