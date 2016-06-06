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
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlServerServiceCollectionExtensions
    {
         public static IServiceCollection AddEntityFrameworkSqlServerLazy(this IServiceCollection services)
        {
            services.AddEntityFrameworkSqlServer();

            services.Replace(new ServiceDescriptor(typeof(IModelSource), typeof(MaterializingSqlServerModelSource), ServiceLifetime.Singleton));
            services.Replace(new ServiceDescriptor(typeof(RelationalEntityQueryableExpressionVisitorFactory), typeof(MaterializingRelationalEntityQueryableExpressionVisitorFactory), ServiceLifetime.Scoped));
            services.Replace(new ServiceDescriptor(typeof(IStateManager), typeof(LazyStateManager), ServiceLifetime.Scoped));
            services.Replace(new ServiceDescriptor(typeof(IDbSetSource), typeof(EncapsulatedDbSetSource), ServiceLifetime.Singleton));
            services.AddSingleton<SqlServerModelSource, MaterializingSqlServerModelSource>();
            services.AddSingleton<IProxyBuilder, ProxyBuilder>();
            services.AddSingleton<IBuilderProvider, BuilderProvider>();

            return services;
        }
    }
}
