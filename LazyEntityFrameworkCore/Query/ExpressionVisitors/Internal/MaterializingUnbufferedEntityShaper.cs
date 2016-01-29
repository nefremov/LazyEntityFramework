using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Clauses;

namespace LazyEntityFrameworkCore.Query.ExpressionVisitors.Internal
{
    public class MaterializingUnbufferedEntityShaper<TEntity> : UnbufferedEntityShaper<TEntity> where TEntity : class
    {
        public MaterializingUnbufferedEntityShaper(IQuerySource querySource, string entityType, bool trackingQuery, IKey key, Func<ValueBuffer, object> materializer) 
            : base(querySource, entityType, trackingQuery, key, materializer)
        {
        }

        public override TEntity Shape(QueryContext queryContext, ValueBuffer valueBuffer)
        {
            if (IsTrackingQuery)
            {
                var entry = queryContext.StateManager.TryGetEntry(Key, valueBuffer, !AllowNullResult);

                if (entry != null)
                {
                    return (TEntity)entry.Entity;
                }
            }
            List<object> values = new List<object>(valueBuffer.Count + 1);
            for (int i = 0; i < valueBuffer.Count; i++)
            {
                values.Add(valueBuffer[i]);
            }
            values.Add(queryContext.StateManager.Context);
            ValueBuffer temp = new ValueBuffer(values);
            return (TEntity)Materializer(temp);
        }

        public override IShaper<TDerived> Cast<TDerived>()
            => new MaterializingUnbufferedOffsetEntityShaper<TDerived>(
                QuerySource,
                EntityType,
                IsTrackingQuery,
                Key,
                Materializer);

        public override EntityShaper WithOffset(int offset)
            => new MaterializingUnbufferedOffsetEntityShaper<TEntity>(
                QuerySource,
                EntityType,
                IsTrackingQuery,
                Key,
                Materializer)
                .SetOffset(offset);
    }
}