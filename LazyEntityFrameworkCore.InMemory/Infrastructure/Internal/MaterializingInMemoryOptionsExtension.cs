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
        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkInMemoryDatabaseLazy();
        }
    }
}
