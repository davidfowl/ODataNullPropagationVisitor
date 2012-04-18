using System.Linq;
using QueryInterceptor;

namespace OData.Linq {
    public static class QueryableExtensions {
        public static IQueryable<T> WithoutNullPropagation<T>(this IQueryable<T> query) {
            return query.InterceptWith(new NullPropagationYanker());
        }

        public static IQueryable<T> WithoutMethodCallNullPropagation<T>(this IQueryable<T> query) {
            return query.InterceptWith(new MethodCallNullPropagationYanker());
        }
    }
}