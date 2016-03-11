using System.Linq.Expressions;
using LazyEntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors
{
    public class MaterializingInMemoryEntityQueryableExpressionVisitorFactory : InMemoryEntityQueryableExpressionVisitorFactory
    {
        private readonly IModel _model;
        private readonly IMaterializerFactory _materializerFactory;

        public MaterializingInMemoryEntityQueryableExpressionVisitorFactory(IModel model,IMaterializerFactory materializerFactory) 
            : base (model, materializerFactory)
        {
            _model = model;
            _materializerFactory = materializerFactory;
        }

        public override ExpressionVisitor Create(
            EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource)
            => new MaterializingInMemoryEntityQueryableExpressionVisitor(
                _model,
                _materializerFactory,
                queryModelVisitor,
                querySource);
    }
}
