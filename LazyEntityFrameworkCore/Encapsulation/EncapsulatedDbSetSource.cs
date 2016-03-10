using System;
using System.Collections.Concurrent;
using System.Reflection;
using LazyEntityFrameworkCore.Lazy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace LazyEntityFrameworkCore.Encapsulation
{
    public class EncapsulatedDbSetSource : IDbSetSource
    {
        private static readonly MethodInfo _genericCreate
            = typeof(EncapsulatedDbSetSource).GetTypeInfo().GetDeclaredMethod(nameof(EncapsulatedDbSetSource.CreateConstructor));

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
            => c => new EncapsulatedDbSet<TEntity>(c);
    }
}
