using System;
using System.Linq;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyEntityFrameworkCore.Lazy;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.ExplicitProxies
{
    public class PostProxy : Post
    {
        private readonly LazyReference<Blog> _localBlog;

        public PostProxy(DbContext context)
        {
            _localBlog = new LazyReference<Blog>(context, b => b.BlogId == BlogId, () => base.Blog);
        }

        public override Blog Blog => _localBlog.Value;
    }
}