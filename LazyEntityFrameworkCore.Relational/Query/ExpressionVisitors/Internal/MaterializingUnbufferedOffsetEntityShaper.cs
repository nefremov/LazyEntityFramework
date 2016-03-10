using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors.Internal
{
    public class MaterializingUnbufferedOffsetEntityShaper<TEntity> : MaterializingUnbufferedEntityShaper<TEntity>
        where TEntity : class
    {
        public MaterializingUnbufferedOffsetEntityShaper(
            IQuerySource querySource,
            string entityType,
            bool trackingQuery,
            IKey key,
            Func<ValueBuffer, object> materializer)
            : base(querySource, entityType, trackingQuery, key, materializer)
        {
        }

        public override TEntity Shape(QueryContext queryContext, ValueBuffer valueBuffer)
            => base.Shape(queryContext, valueBuffer.WithOffset(ValueBufferOffset));
    }
}