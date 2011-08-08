using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace OData.Linq {
    internal class QueryTranslatorProvider<T> : ExpressionVisitor, IQueryProvider {
        private readonly IQueryable _source;
        private bool _yankingNull;

        public QueryTranslatorProvider(IQueryable source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            _source = source;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            return new QueryTranslator<TElement>(_source, expression) as IQueryable<TElement>;
        }

        public IQueryable CreateQuery(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Type elementType = expression.Type.GetGenericArguments().First();
            IQueryable result = (IQueryable)Activator.CreateInstance(typeof(QueryTranslator<>).MakeGenericType(elementType),
                    new object[] { _source, expression });
            return result;
        }

        public TResult Execute<TResult>(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }
            object result = (this as IQueryProvider).Execute(expression);
            return (TResult)result;
        }

        public object Execute(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Expression translated = Visit(expression);
            return _source.Provider.Execute(translated);
        }

        internal IEnumerable ExecuteEnumerable(Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }

            Expression translated = Visit(expression);
            return _source.Provider.CreateQuery(translated);
        }

        protected override Expression VisitConstant(ConstantExpression c) {
            // Fix up the Expression tree to work with the underlying LINQ provider again
            if (c.Type.IsGenericType && 
                c.Type.GetGenericTypeDefinition() == typeof(QueryTranslator<>)) {
                return _source.Expression;
            }
            else {
                return base.VisitConstant(c);
            }
        }

        protected override Expression VisitUnary(UnaryExpression node) {
            if (_yankingNull &&
                node.NodeType == ExpressionType.Convert &&
                Nullable.GetUnderlyingType(node.Type) == typeof(bool)) {
                return Visit(node.Operand);
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node) {
            if (_yankingNull) {
                Expression left = Visit(node.Left);
                Expression right = Visit(node.Right);

                if (left == null) {
                    return right;
                }

                if (right == null) {
                    return left;
                }

                if (node.NodeType == ExpressionType.NotEqual &&
                    (IsNullConstant(right) || IsNullConstant(left))) {
                    return null;
                }

                return Expression.MakeBinary(node.NodeType, left, right);
            }
            return base.VisitBinary(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node) {
            Expression expression;
            if (TryRemoveNullPropagation(node, out expression)) {
                return expression;
            }

            if (_yankingNull && IsNullCheck(node.Test)) {
                return Visit(node.IfFalse);
            }

            return base.VisitConditional(node);
        }

        private bool IsNullCheck(Expression expression) {
            if (expression.NodeType != ExpressionType.Equal) {
                return false;
            }

            var binaryExpr = (BinaryExpression)expression;
            return IsNullConstant(binaryExpr.Right);
        }

        private bool TryRemoveNullPropagation(ConditionalExpression node, out Expression condition) {
            condition = null;
            if (node.IfTrue.NodeType != ExpressionType.Constant) {
                return false;
            }

            if (node.Test.NodeType != ExpressionType.Equal) {
                return false;
            }

            var test = (BinaryExpression)node.Test;
            var constantExpr = (ConstantExpression)node.IfTrue;

            if (constantExpr.Type != typeof(bool)) {
                return false;
            }

            if ((bool)constantExpr.Value == true) {
                return false;
            }

            if (node.IfFalse.NodeType != ExpressionType.MemberAccess) {
                return false;
            }

            var memberExpr = (MemberExpression)node.IfFalse;

            if (!memberExpr.Member.DeclaringType.IsGenericType ||
                memberExpr.Member.DeclaringType.GetGenericTypeDefinition() != typeof(Nullable<>)) {
                return false;
            }

            if (memberExpr.Expression != test.Left) {
                return false;
            }

            // After detecting the null propagation expression, proceed to remove the null guards

            _yankingNull = true;
            condition = Visit(memberExpr.Expression);
            _yankingNull = false;
            return true;
        }

        private bool IsNullConstant(Expression expression) {
            return expression.NodeType == ExpressionType.Constant &&
                   ((ConstantExpression)expression).Value == null;
        }
    }
}