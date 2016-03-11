// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using LazyEntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors.Internal
{
    public class MaterializingInMemoryEntityQueryableExpressionVisitor : InMemoryEntityQueryableExpressionVisitor
    {
        private readonly IModel _model;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly IQuerySource _querySource;

        public MaterializingInMemoryEntityQueryableExpressionVisitor(
            IModel model,
            IMaterializerFactory materializerFactory,
            EntityQueryModelVisitor entityQueryModelVisitor,
            IQuerySource querySource)
            : base(model, materializerFactory, entityQueryModelVisitor, querySource)
        {

            _model = model;
            _materializerFactory = materializerFactory;
            _querySource = querySource;
        }

        protected override Expression VisitEntityQueryable(Type elementType)
        {
            var entityType = _model.FindEntityType(elementType);

            if (QueryModelVisitor.QueryCompilationContext
                .QuerySourceRequiresMaterialization(_querySource))
            {
                var materializer = _materializerFactory.CreateMaterializer(entityType);

                return Expression.Call(
                    MaterializingInMemoryQueryModelVisitor.EntityQueryMethodInfo.MakeGenericMethod(elementType),
                    EntityQueryModelVisitor.QueryContextParameter,
                    Expression.Constant(entityType),
                    Expression.Constant(entityType.FindPrimaryKey()),
                    materializer,
                    Expression.Constant(QueryModelVisitor.QueryCompilationContext.IsTrackingQuery));
            }

            return Expression.Call(
                MaterializingInMemoryQueryModelVisitor.ProjectionQueryMethodInfo,
                EntityQueryModelVisitor.QueryContextParameter,
                Expression.Constant(entityType));
        }
    }
}
