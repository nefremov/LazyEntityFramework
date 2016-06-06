using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace LazyEntityFrameworkCore.Infrastructure.Internal
{
    public class MaterializingSqlServerOptionsExtension : SqlServerOptionsExtension
    {
        public MaterializingSqlServerOptionsExtension() { }
        public MaterializingSqlServerOptionsExtension(SqlServerOptionsExtension from) : base(from) { }
        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkSqlServerLazy();
        }
    }
}