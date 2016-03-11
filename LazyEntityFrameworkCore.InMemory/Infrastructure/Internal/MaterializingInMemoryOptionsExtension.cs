using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyEntityFrameworkCore.Encapsulation;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using LazyEntityFrameworkCore.Lazy.Proxy;
using LazyEntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LazyEntityFrameworkCore.Infrastructure.Internal
{
    public class MaterializingInMemoryOptionsExtension : InMemoryOptionsExtension
    {
        public MaterializingInMemoryOptionsExtension() {}
        public MaterializingInMemoryOptionsExtension(InMemoryOptionsExtension copyFrom) : base (copyFrom)
        {
        }
        public override void ApplyServices(EntityFrameworkServicesBuilder builder)
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
        }
    }
}
