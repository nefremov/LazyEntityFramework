using System.Linq;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.ExplicitProxies
{
    public class PostProxy : Post
    {
        private readonly BloggingContext _context;
        private readonly LazyStateManager _stateManager;

        public PostProxy(DbContext context)
        {
            _context = (BloggingContext)context;
            _stateManager = (LazyStateManager)context.GetService<IStateManager>();
        }

        private bool _blogLoaded = false;
        public override Blog Blog
        {
            get
            {
                if (!_blogLoaded && !_stateManager.InTracking)
                {
                    _context.Blogs.Where(b => b.BlogId == BlogId).Load();
                    _blogLoaded = true;
                }
                return base.Blog;
            }
        }
    }
}