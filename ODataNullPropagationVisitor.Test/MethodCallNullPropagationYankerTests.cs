using System.Collections.Generic;
using System.Linq;
using Xunit;
using OData.Linq;
using System;
using QueryInterceptor;

namespace ODataNullPropagationVisitor.Test
{
    public class MethodCallNullPropagationYankerTests
    {
        public class Product
        {
            public string Name { get; set; }
        }

        [Fact]
        public void Should_Remove_Null_Propgation_In_MethodCall_Expressions()
        {
            List<Product> productList = new List<Product>()
            {
                new Product() { Name = "Product 1" },
                new Product() { Name = null }
            };

            var productsQueryable = productList.AsQueryable();

            var withPropagation = (from p in productsQueryable
                                   where (p.Name == null ? "product 1" : p.Name.ToLower()) == "product 1"
                                   select p).Expression;

            var withoutPropagation = (from p in productsQueryable
                                      where p.Name.ToLower() == "product 1"
                                      select p).Expression;

            var propagationRemoved = new MethodCallNullPropagationYanker().Visit(withPropagation);

            string withPropagationString = SimpleExpressionPrinter.Stringify(withPropagation);
            string withoutPropagationString = SimpleExpressionPrinter.Stringify(withoutPropagation);
            string propagationRemovedString = SimpleExpressionPrinter.Stringify(propagationRemoved);

            Assert.NotEqual(withPropagationString, propagationRemovedString);
            Assert.Equal(withoutPropagationString, propagationRemovedString);
        }
    }
}
