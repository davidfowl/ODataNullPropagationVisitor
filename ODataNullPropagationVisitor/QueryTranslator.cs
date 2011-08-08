using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OData.Linq {
    internal class QueryTranslator<T> : IOrderedQueryable<T> {
        private readonly Expression _expression;
        private readonly QueryTranslatorProvider<T> _provider;

        public QueryTranslator(IQueryable source) {
            _expression = Expression.Constant(this);
            _provider = new QueryTranslatorProvider<T>(source);
        }

        public QueryTranslator(IQueryable source, Expression expression) {
            if (expression == null) {
                throw new ArgumentNullException("expression");
            }
            _expression = expression;
            _provider = new QueryTranslatorProvider<T>(source);
        }

        public IEnumerator<T> GetEnumerator() {
            return ((IEnumerable<T>)_provider.ExecuteEnumerable(_expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _provider.ExecuteEnumerable(_expression).GetEnumerator();
        }

        public Type ElementType {
            get { return typeof(T); }
        }

        public Expression Expression {
            get { return _expression; }
        }

        public IQueryProvider Provider {
            get { return _provider; }
        }
    }    
}