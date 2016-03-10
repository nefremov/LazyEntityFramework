using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyEntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyEntityFrameworkCore.Extensions
{
    public static class InMemoryDbContextOptionsExtensions
    {
        public static InMemoryDbContextOptionsBuilder UseInMemoryDatabaseLazy(this DbContextOptionsBuilder optionsBuilder)
        {
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MaterializingInMemoryOptionsExtension());

            return new InMemoryDbContextOptionsBuilder(optionsBuilder);
        }
    }
}
