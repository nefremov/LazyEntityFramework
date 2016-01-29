using System.Collections.Generic;
using System.Linq;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.ExplicitProxies
{
    public class BlogProxy : Blog
    {
        private readonly BloggingContext _context;
        private readonly LazyStateManager _stateManager;

        public BlogProxy(DbContext context)
        {
            _context = (BloggingContext)context;
            _stateManager = (LazyStateManager)context.GetService<IStateManager>();
        }

        private bool _postsLoaded = false;

        public int PostsCount => base.Posts.Count; // just for sample code to demonstrate partial loading due to change tracking
        public override ICollection<Post> Posts
        {
            get
            {
                if (!_postsLoaded && !_stateManager.InTracking)

                {
                    _context.Posts.Where(p => p.BlogId == BlogId).Load();
                    _postsLoaded = true;
                }
                return base.Posts;
            }
        }
    }
}