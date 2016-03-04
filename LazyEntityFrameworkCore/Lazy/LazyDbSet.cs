using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;

namespace LazyEntityFrameworkCore.Lazy
{
    public class LazyDbSet<TEntity> : InternalDbSet<TEntity> where TEntity : class
    {
        public LazyDbSet(DbContext context) : base(context)
        {
        }

        public override EntityEntry<TEntity> Add(TEntity entity)
        {
            throw new InvalidOperationException("not supported");
        }

        public override void AddRange(IEnumerable<TEntity> entities)
        {
            throw new InvalidOperationException("not supported");
        }

        public override void AddRange(params TEntity[] entities)
        {
            throw new InvalidOperationException("not supported");
        }

        public override EntityEntry<TEntity> Attach(TEntity entity)
        {
            throw new InvalidOperationException("not supported");
        }

        public override void AttachRange(IEnumerable<TEntity> entities)
        {
            throw new InvalidOperationException("not supported");
        }

        public override void AttachRange(params TEntity[] entities)
        {
            throw new InvalidOperationException("not supported");
        }
    }
}
