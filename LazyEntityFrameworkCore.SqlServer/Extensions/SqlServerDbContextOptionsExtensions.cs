using System;
using System.Data.Common;
using LazyEntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;

namespace LazyEntityFrameworkCore.Extensions
{
    public static class SqlServerDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseSqlServerLazy(
            this DbContextOptionsBuilder optionsBuilder,
            string connectionString,
            Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
        {
            var extension = GetOrCreateExtension(optionsBuilder);
            extension.ConnectionString = connectionString;
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDbContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseSqlServerLazy(
            this DbContextOptionsBuilder optionsBuilder,
            DbConnection connection,
            Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
        {
            var extension = GetOrCreateExtension(optionsBuilder);
            extension.Connection = connection;
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDbContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseSqlServerLazy<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString,
            Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSqlServerLazy(
                (DbContextOptionsBuilder)optionsBuilder, connectionString, sqlServerOptionsAction);

        public static DbContextOptionsBuilder<TContext> UseSqlServerLazy<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            DbConnection connection,
            Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSqlServerLazy(
                (DbContextOptionsBuilder)optionsBuilder, connection, sqlServerOptionsAction);

        private static SqlServerOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        {
            var existing = optionsBuilder.Options.FindExtension<MaterializingSqlServerOptionsExtension>();
            return existing != null
                ? new MaterializingSqlServerOptionsExtension(existing)
                : new MaterializingSqlServerOptionsExtension();
        }

        private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
        {
            // Set warnings defaults
            optionsBuilder.ConfigureWarnings(w =>
            {
                w.Configuration.TryAddExplicit(
                    RelationalEventId.AmbientTransactionWarning, WarningBehavior.Throw);
            });
        }
    }
}