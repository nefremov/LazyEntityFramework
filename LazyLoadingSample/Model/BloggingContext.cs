using System;
using System.Linq.Expressions;
using LazyEntityFrameworkCore;
using LazyEntityFrameworkCore.Lazy.Proxy;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using LazyEntityFrameworkCore.Extensions;
using LazyLoadingSample.Builders;
using LazyLoadingSample.ExplicitProxies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
namespace LazyLoadingSample.Model
{
    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        public BloggingContext() : base()
        {
        }
        public BloggingContext(DbContextOptions options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Visual Studio 2015 | Use the LocalDb 12 instance created by Visual Studio
            optionsBuilder.UseSqlServerWithMaterialization(@"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;");
            //optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;");
            // Visual Studio 2013 | Use the LocalDb 11 instance created by Visual Studio
            //optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Make Blog.Url required
            modelBuilder.Entity<Blog>().HasKey(b => b.BlogId);
            modelBuilder.Entity<Blog>().Property(b => b.BlogId).HasColumnName("BlogId").HasAnnotation("BackingField", "_blogId");
            modelBuilder.Entity<Blog>().Property(b => b.Url).HasColumnName("Url").HasAnnotation("BackingField", "_url");
            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Posts)
                .WithOne(p => p.Blog)
                .HasForeignKey(p => p.BlogId).HasAnnotation("BackingField", "_posts").HasAnnotation("InverseField", "_Blog")
                .HasPrincipalKey(b => b.BlogId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Post>().HasKey(p => p.PostId);
            modelBuilder.Entity<Post>().Property(b => b.PostId).HasColumnName("PostId").HasAnnotation("BackingField", "_PostId");
            modelBuilder.Entity<Post>().Property(b => b.BlogId).HasColumnName("BlogId").HasAnnotation("BackingField", "_BlogId");
            modelBuilder.Entity<Post>().Property(b => b.Title).HasColumnName("Title").HasAnnotation("BackingField", "_Title");
            modelBuilder.Entity<Post>().Property(b => b.Content).HasColumnName("Content").HasAnnotation("BackingField", "_Content");
            //modelBuilder.Entity<Post>()
            //    .HasOne(b => b.Blog)
            //    .WithMany(p => p.Posts)
            //    .HasForeignKey(p => p.BlogId)
            //    .HasPrincipalKey(b => b.BlogId).OnDelete(DeleteBehavior.Restrict);
            this.GetService<IProxyBuilder>().RegisterProxy<Blog, BlogProxy>().RegisterProxy<Post, PostProxy>();
            this.GetService<IBuilderProvider>().Register<Blog, BlogBuilder>().Register<Post, PostBuilder>();
        }

        public BlogBuilder CreateBlog()
        {
            return (BlogBuilder)new BlogBuilder(this);
        }
    }
}