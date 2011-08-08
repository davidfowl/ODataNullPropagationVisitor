using System.Linq;

namespace OData.Linq {
    public static class QueryableExtensions {
        public static IQueryable<T> WithoutNullPropagation<T>(this IQueryable<T> query) {
            return new QueryTranslator<T>(query);
        }
    }
}