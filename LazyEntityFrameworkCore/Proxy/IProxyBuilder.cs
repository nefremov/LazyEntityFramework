using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.Proxy
{
    public interface IProxyBuilder
    {
        Type GetProxy(Type entityType);
        IProxyBuilder RegisterProxy(Type entityType, Type proxyType);
        IProxyBuilder RegisterProxy<TEntity, TProxy>();
        object ConstructProxy(EntityType entityType, ValueBuffer valueBuffer, DbContext context);
    }
}
