using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.Lazy.Proxy
{
    public interface IProxyBuilder
    {
        Type GetProxy(Type entityType);
        IProxyBuilder RegisterProxy(Type entityType, Type proxyType);
        IProxyBuilder RegisterProxy<TEntity, TProxy>();
        object ConstructProxy(EntityType entityType, ValueBuffer valueBuffer, DbContext context);
        object ConstructProxy(IEntityType entityType, DbContext context);
    }
}
