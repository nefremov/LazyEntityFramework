using LazyEntityFrameworkCore;
using LazyEntityFrameworkCore.Proxy;
using LazyLoadingSample.ExplicitProxies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.Model
{
    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        public BloggingContext()
        {

        }
        public BloggingContext(DbContextOptions options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Visual Studio 2015 | Use the LocalDb 12 instance created by Visual Studio
            optionsBuilder.UseSqlServerWithMaterialization(@"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;");
            // Visual Studio 2013 | Use the LocalDb 11 instance created by Visual Studio
            //optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Make Blog.Url required
            modelBuilder.Entity<Blog>()
                .Property(b => b.Url)
                .IsRequired();
            this.GetService<IProxyBuilder>().RegisterProxy<Blog, BlogProxy>().RegisterProxy<Post, PostProxy>();
        }
    }
}