using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.Query.Internal
{
    public class MaterializingInMemoryQueryModelVisitor : InMemoryQueryModelVisitor
    {
        public MaterializingInMemoryQueryModelVisitor(IQueryOptimizer queryOptimizer, INavigationRewritingExpressionVisitorFactory navigationRewritingExpressionVisitorFactory, ISubQueryMemberPushDownExpressionVisitor subQueryMemberPushDownExpressionVisitor, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory, IEntityResultFindingExpressionVisitorFactory entityResultFindingExpressionVisitorFactory, ITaskBlockingExpressionVisitor taskBlockingExpressionVisitor, IMemberAccessBindingExpressionVisitorFactory memberAccessBindingExpressionVisitorFactory, IOrderingExpressionVisitorFactory orderingExpressionVisitorFactory, IProjectionExpressionVisitorFactory projectionExpressionVisitorFactory, IEntityQueryableExpressionVisitorFactory entityQueryableExpressionVisitorFactory, IQueryAnnotationExtractor queryAnnotationExtractor, IResultOperatorHandler resultOperatorHandler, IEntityMaterializerSource entityMaterializerSource, IExpressionPrinter expressionPrinter, IMaterializerFactory materializerFactory, QueryCompilationContext queryCompilationContext) : base(queryOptimizer, navigationRewritingExpressionVisitorFactory, subQueryMemberPushDownExpressionVisitor, querySourceTracingExpressionVisitorFactory, entityResultFindingExpressionVisitorFactory, taskBlockingExpressionVisitor, memberAccessBindingExpressionVisitorFactory, orderingExpressionVisitorFactory, projectionExpressionVisitorFactory, entityQueryableExpressionVisitorFactory, queryAnnotationExtractor, resultOperatorHandler, entityMaterializerSource, expressionPrinter, materializerFactory, queryCompilationContext)
        {
        }

        public new static readonly MethodInfo EntityQueryMethodInfo
            = typeof(InMemoryQueryModelVisitor).GetTypeInfo()
                .GetDeclaredMethod(nameof(EntityQuery));

        private static IEnumerable<TEntity> EntityQuery<TEntity>(
            QueryContext queryContext,
            IEntityType entityType,
            IKey key,
            Func<IEntityType, ValueBuffer, object> materializer,
            bool queryStateManager)
            where TEntity : class
        {
            return ((InMemoryQueryContext)queryContext).Store
                .GetTables(entityType)
                .SelectMany(t =>
                    t.Rows.Select(vs =>
                    {
                        var context = queryContext.StateManager.Context;
                        var vals = new List<object>(vs.Length + 1);
                        vals.AddRange(vs);
                        vals.Add(context);
                        var valueBuffer = new ValueBuffer(vals);

                        return (TEntity)queryContext
                            .QueryBuffer
                            .GetEntity(
                                key,
                                new EntityLoadInfo(
                                    valueBuffer,
                                    vr => materializer(t.EntityType, vr)),
                                queryStateManager,
                                throwOnNullKey: false);
                    }));
        }
    }
}
