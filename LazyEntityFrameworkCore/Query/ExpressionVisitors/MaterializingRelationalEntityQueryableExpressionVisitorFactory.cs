using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors
{
    public class MaterializingRelationalEntityQueryableExpressionVisitorFactory : RelationalEntityQueryableExpressionVisitorFactory
    {
        private readonly IModel _model;
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly IShaperCommandContextFactory _shaperCommandContextFactory;
        private readonly IRelationalAnnotationProvider _relationalAnnotationProvider;

        public MaterializingRelationalEntityQueryableExpressionVisitorFactory(IModel model, ISelectExpressionFactory selectExpressionFactory, IMaterializerFactory materializerFactory, 
            IShaperCommandContextFactory shaperCommandContextFactory, IRelationalAnnotationProvider relationalAnnotationProvider) : base(model, selectExpressionFactory, materializerFactory, shaperCommandContextFactory, relationalAnnotationProvider)
        {
            _model = model;
            _selectExpressionFactory = selectExpressionFactory;
            _materializerFactory = materializerFactory;
            _shaperCommandContextFactory = shaperCommandContextFactory;
            _relationalAnnotationProvider = relationalAnnotationProvider;
        }

        public override ExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource)
        {
            return new MaterializingRelationalEntityQueryableExpressionVisitor(
                _model,
                _selectExpressionFactory,
                _materializerFactory,
                _shaperCommandContextFactory,
                _relationalAnnotationProvider,
                (RelationalQueryModelVisitor)queryModelVisitor,
                querySource); ;
        }
    }
}