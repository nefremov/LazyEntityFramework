using System;
using System.Linq;
using LazyLoadingSample.ExplicitProxies;
using LazyLoadingSample.Model;

namespace LazyLoadingSample
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new BloggingContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                var blog = new Blog { Url = "http://blogs.msdn.com/adonet" };
                db.Blogs.Add(blog);
                //blog.Posts.Add(new Post {Title ="Title"});
                db.Posts.Add(new Post { Blog = blog, Title = "Title 1" });
                db.Posts.Add(new Post { Blog = blog, Title = "Title 2" });
                var count = db.SaveChanges();
                Console.WriteLine("{0} records saved to database", count);
            }

            using (var db = new BloggingContext())
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
