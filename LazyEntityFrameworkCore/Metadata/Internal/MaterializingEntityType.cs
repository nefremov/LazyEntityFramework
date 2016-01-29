using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LazyEntityFrameworkCore.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class MaterializingEntityType : EntityType, IEntityMaterializer
    {
        private IProxyBuilder _proxyBuilder;
        public MaterializingEntityType(string name, Model model, ConfigurationSource configurationSource) : base(name, model, configurationSource)
        {
        }

        public object CreateEntity(ValueBuffer valueBuffer)
        {
            DbContext context = (DbContext) valueBuffer[valueBuffer.Count - 1];
            if (_proxyBuilder == null)
            {
                _proxyBuilder = context.GetInfrastructure().GetService<IProxyBuilder>();
            }

            return _proxyBuilder.ConstructProxy(this, valueBuffer, context);
        }
    }
}