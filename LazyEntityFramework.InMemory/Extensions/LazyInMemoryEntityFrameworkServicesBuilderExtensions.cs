using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyEntityFrameworkCore.Encapsulation;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using LazyEntityFrameworkCore.Infrastructure.Internal;
using LazyEntityFrameworkCore.Lazy.Proxy;
using LazyEntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LazyInMemoryEntityFrameworkServicesBuilderExtensions
    {
        public static EntityFrameworkServicesBuilder AddInMemoryDatabaseLazy(this EntityFrameworkServicesBuilder builder)
        {
            builder.AddInMemoryDatabase();
            var services = builder.GetInfrastructure();

            services.Replace(new ServiceDescriptor(typeof(IModelSource), typeof(MaterializingInMemoryModelSource), ServiceLifetime.Singleton));
            services.Replace(new ServiceDescriptor(typeof(InMemoryEntityQueryableExpressionVisitorFactory), typeof(MaterializingInMemoryEntityQueryableExpressionVisitorFactory), ServiceLifetime.Scoped));
            services.Replace(new ServiceDescriptor(typeof(IStateManager), typeof(LazyStateManager), ServiceLifetime.Scoped));
            services.Replace(new ServiceDescriptor(typeof(IDbSetSource), typeof(EncapsulatedDbSetSource), ServiceLifetime.Singleton));
            services.AddSingleton<InMemoryModelSource, MaterializingInMemoryModelSource>();
            services.AddSingleton<IProxyBuilder, ProxyBuilder>();
            services.AddSingleton<IBuilderProvider, BuilderProvider>();

            return builder;
        }
    }
}
