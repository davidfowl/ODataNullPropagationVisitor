using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ODataNullPropagationVisitor.Test
{
    internal class SimpleExpressionPrinter : ExpressionVisitor {
        private StringBuilder _sb = new StringBuilder();

        public override Expression Visit(Expression node) {
            if (node == null)
                return node;

            _sb.AppendFormat(" {0}", node.GetType().Name);
            return base.Visit(node);
        }

        public static string Stringify(Expression node) {
            var printer = new SimpleExpressionPrinter();
            printer.Visit(node);
            return printer._sb.ToString().Trim();
        }
    }
}
