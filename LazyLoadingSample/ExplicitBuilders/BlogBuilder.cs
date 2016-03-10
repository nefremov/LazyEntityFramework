using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyLoadingSample.Builders
{
    public class BlogBuilder : EntityBuilder<Blog>
    {
        private DbContext _Context;

        public BlogBuilder(DbContext context) : base(context)
        {
            _Context = context;
            Requires(b => b.Url);
            //Requires(b => b.BlogId);
        }

        protected override Blog Construct()
        {
            var blog = base.Construct();
            _Context.Add(blog);
            return blog;
        }
    }
}
