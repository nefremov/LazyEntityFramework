using System;
using System.Collections.Concurrent;
using System.Reflection;
using LazyEntityFrameworkCore.Lazy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace LazyEntityFrameworkCore.Internal
{
    public class LazyDbSetSource : IDbSetSource
    {
        private static readonly MethodInfo _genericCreate
            = typeof(LazyDbSetSource).GetTypeInfo().GetDeclaredMethod(nameof(LazyDbSetSource.CreateConstructor));

        // Stores DbSet<T> objects
        private readonly ConcurrentDictionary<Type, Func<DbContext, object>> _cache
            = new ConcurrentDictionary<Type, Func<DbContext, object>>();

        //[CallsMakeGenericMethod(nameof(CreateConstructor), typeof(TypeArgumentCategory.EntityTypes))]
        public virtual object Create(DbContext context, Type type)
            => _cache.GetOrAdd(
                type,
                t => (Func<DbContext, object>)_genericCreate.MakeGenericMethod(type).Invoke(null, null))(context);

        //[UsedImplicitly]
        private static Func<DbContext, object> CreateConstructor<TEntity>() where TEntity : class
            => c => new LazyDbSet<TEntity>(c);
    }
}
