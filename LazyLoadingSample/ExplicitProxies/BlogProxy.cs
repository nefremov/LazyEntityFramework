using System.Collections.Generic;
using System.Linq;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyEntityFrameworkCore.Encapsulation;
using LazyEntityFrameworkCore.Lazy;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.ExplicitProxies
{
    public class BlogProxy : Blog
    {
        private readonly LazyCollection<Post, Blog> _localPosts;

        public BlogProxy(DbContext context)
        {
            var postsNavigation =
                context.Model.FindEntityType(typeof (Blog))
                    .GetNavigations()
                    .First(n => n.ForeignKey.DeclaringEntityType.ClrType == typeof (Post));
            _localPosts = new LazyCollection<Post, Blog>(context, postsNavigation, this, p=>p.BlogId == BlogId);
        }

        public int PostsCount => base.Posts.Count; // just for sample code to demonstrate partial loading due to change tracking

        public override IEncapsulatedCollection<Post> Posts => _localPosts;
    }
}