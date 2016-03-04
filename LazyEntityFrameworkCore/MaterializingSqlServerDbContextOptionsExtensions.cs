using LazyEntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyEntityFrameworkCore
{
    public static class MaterializingSqlServerDbContextOptionsExtensions
    {
        /// <summary>
        ///     Configures the context to connect to a Microsoft SQL Server database.
        /// </summary>
        /// <param name="optionsBuilder"> The options for the context. </param>
        /// <param name="connectionString"> The connection string of the database to connect to. </param>
        /// <returns> An options builder to allow additional SQL Server specific configuration. </returns>
        public static SqlServerDbContextOptionsBuilder UseSqlServerWithMaterialization(
            this DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            var extension = GetOrCreateExtension(optionsBuilder);
            extension.ConnectionString = connectionString;
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return new SqlServerDbContextOptionsBuilder(optionsBuilder);
        }


        private static MaterializingSqlServerOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        {
            var existing = optionsBuilder.Options.FindExtension<MaterializingSqlServerOptionsExtension>();
            return existing != null
                ? new MaterializingSqlServerOptionsExtension(existing)
                : new MaterializingSqlServerOptionsExtension();
        }
    }
}