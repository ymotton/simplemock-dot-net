using System;
using System.Linq.Expressions;

namespace SimpleMock
{
    class ExpressionHelpers
    {
        public static void GetArgumentValueAndType(Expression expression, out object value, out Type type)
        {
            if (expression is ConstantExpression)
            {
                GetConstant(
                    expression,
                    out value,
                    out type
                );
            }
            else if (expression is UnaryExpression)
            {
                GetUnary(
                    expression,
                    out value,
                    out type
                );
            }
            else if (expression is MemberExpression)
            {
                GetMember(
                    expression,
                    out value,
                    out type
                );
            }
            else
            {
                throw new InvalidOperationException("Expression Not supported");
            }

        }
        private static void GetConstant(Expression expression, out object value, out Type type)
        {
            var constantExpression = expression as ConstantExpression;

            if (constantExpression == null)
            {
                throw new ArgumentException("expression");
            }

            type = constantExpression.Type;
            value = constantExpression.Value;
        }
        private static void GetUnary(Expression expression, out object value, out Type type)
        {
            var unaryExpression = expression as UnaryExpression;

            if (unaryExpression == null)
            {
                throw new ArgumentException("expression");
            }

            var constantExpression = unaryExpression.Operand as ConstantExpression;

            if (constantExpression == null)
            {
                throw new NotSupportedException(
                    string.Format(
                        "This type of Operand is not supported for UnaryExpressions ({0})",
                        unaryExpression.Operand.GetType())
                );
            }

            type = typeof(Nullable<>).MakeGenericType(constantExpression.Type);
            value = constantExpression.Value;
        }
        private static void GetMember(Expression expression, out object value, out Type type)
        {
            var memberExpression = expression as MemberExpression;

            if (memberExpression == null)
            {
                throw new ArgumentException("expression");
            }

            var objectMember = Expression.Convert(memberExpression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            value = getter();
            type = memberExpression.Type;
        }
    }
}
