using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors.Internal
{
    public class MaterializingBufferedEntityShaper<TEntity> : BufferedEntityShaper<TEntity>
        where TEntity : class
    {
        public MaterializingBufferedEntityShaper(
            IQuerySource querySource,
            string entityType,
            bool trackingQuery,
            IKey key,
            Func<ValueBuffer, object> materializer)
            : base(querySource, entityType, trackingQuery, key, materializer)
        {
        }

        public override TEntity Shape(QueryContext queryContext, ValueBuffer valueBuffer)
        {
            List<object> values = new List<object>(valueBuffer.Count + 1);
            for (int i = 0; i < valueBuffer.Count; i++)
            {
                values.Add(valueBuffer[i]);
            }
            values.Add(queryContext.StateManager.Context);
            ValueBuffer temp = new ValueBuffer(values);

            var entity = (TEntity)queryContext.QueryBuffer
                .GetEntity(
                    Key,
                    new EntityLoadInfo(temp, Materializer),
                    queryStateManager: IsTrackingQuery,
                    throwOnNullKey: !AllowNullResult);

            return entity;
        }

        public override IShaper<TDerived> Cast<TDerived>()
            => new MaterializingBufferedOffsetEntityShaper<TDerived>(
                QuerySource,
                EntityType,
                IsTrackingQuery,
                Key,
                Materializer);

        public override EntityShaper WithOffset(int offset)
            => new MaterializingBufferedOffsetEntityShaper<TEntity>(
                QuerySource,
                EntityType,
                IsTrackingQuery,
                Key,
                Materializer)
                .SetOffset(offset);
    }
}