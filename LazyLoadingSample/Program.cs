using System;
using System.Linq;
using LazyEntityFrameworkCore.Infrastructure.Internal;
using LazyLoadingSample.ExplicitProxies;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = @"Server=(localdb)\mssqllocaldb;Database=Ef7Tests;Trusted_Connection=True;";
            DbContextOptionsBuilder<BloggingContext> optionsBuilder = new DbContextOptionsBuilder<BloggingContext>(new DbContextOptions<BloggingContext>());
            var existing = optionsBuilder.Options.FindExtension<MaterializingSqlServerOptionsExtension>();
            var extension = existing != null
                ? new MaterializingSqlServerOptionsExtension(existing)
                : new MaterializingSqlServerOptionsExtension();
            extension.ConnectionString = connectionString;
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            using (var db = new BloggingContext(optionsBuilder.Options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                var blog = db.CreateBlog().Set(b => b.Url, "http://blogs.msdn.com/adonet").Build();
                blog.Posts.GetBuilder().Set(p => p.Title, "Title 1").Set(p => p.Content, "Content 1").Build();
                blog.Posts.GetBuilder().Set(p => p.Title, "Title 2").Set(p => p.Content, "Content 2").Build();
                var count = db.SaveChanges();
                Console.WriteLine("{0} records saved to database", count);
            }

            //using (var db = new BloggingContext())
            //{
            //    db.Database.EnsureDeleted();
            //    db.Database.EnsureCreated();
            //    var blog = new Blog {Url = "http://blogs.msdn.com/adonet"};
            //    db.Blogs.Add(blog);
            //    db.Posts.Add(new Post { Title = "Title 1", Blog = blog });
            //    db.Posts.Add(new Post { Title = "Title 2", Blog = blog });
            //    var count = db.SaveChanges();
            //    Console.WriteLine("{0} records saved to database", count);
            //}

            using (var db = new BloggingContext(optionsBuilder.Options))
            {
                db.ChangeTracker.AutoDetectChangesEnabled = true;
                Blog blog = db.Blogs.First();
                Post post = db.Posts.First();
                Console.WriteLine("Posts collection of the Blog {0} contains {1} posts (due to change tracking).", blog.Url, ((BlogProxy)blog).PostsCount);
                Console.WriteLine("All posts to blog: {0} (iterated with lazy loading)", blog.Url);
                foreach (Post pst in blog.Posts)
                {
                    Console.WriteLine(" - {0}", pst.Title);
                }
                Console.WriteLine("Posts collection of the Blog {0} contains {1} posts (after lazy loading).", blog.Url, ((BlogProxy)blog).PostsCount);
                Console.WriteLine("press any key to exit");
                Console.ReadKey();
            }

        }

    }
}
