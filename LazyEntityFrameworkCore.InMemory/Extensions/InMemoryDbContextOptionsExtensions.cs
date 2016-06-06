using System;
using LazyEntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyEntityFrameworkCore.Extensions
{
    public static class InMemoryDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder<TContext> UseInMemoryDatabaseLazy<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string databaseName,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseInMemoryDatabaseLazy((DbContextOptionsBuilder)optionsBuilder, databaseName, inMemoryOptionsAction);

        public static DbContextOptionsBuilder UseInMemoryDatabaseLazy(
            this DbContextOptionsBuilder optionsBuilder,
            string databaseName,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            var extension = optionsBuilder.Options.FindExtension<MaterializingInMemoryOptionsExtension>();

            extension = extension != null
                ? new MaterializingInMemoryOptionsExtension(extension)
                : new MaterializingInMemoryOptionsExtension();

            if (databaseName != null)
            {
                extension.StoreName = databaseName;
            }

            ConfigureWarnings(optionsBuilder);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            inMemoryOptionsAction?.Invoke(new InMemoryDbContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseInMemoryDatabaseLazy<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseInMemoryDatabaseLazy((DbContextOptionsBuilder)optionsBuilder, inMemoryOptionsAction);

        public static DbContextOptionsBuilder UseInMemoryDatabaseLazy(
            this DbContextOptionsBuilder optionsBuilder,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            ConfigureWarnings(optionsBuilder);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MaterializingInMemoryOptionsExtension());

            inMemoryOptionsAction?.Invoke(new InMemoryDbContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
        {
            // Set warnings defaults
            optionsBuilder.ConfigureWarnings(w =>
            {
                w.Configuration.TryAddExplicit(
                    InMemoryEventId.TransactionIgnoredWarning, WarningBehavior.Throw);
            });
        }
    }
}
