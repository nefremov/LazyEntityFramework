using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using LazyLoadingSample.Model;
using Microsoft.EntityFrameworkCore;

namespace LazyLoadingSample.Builders
{
    public class PostBuilder : EntityBuilder<Post>
    {
        private DbContext _Context;
        public PostBuilder(DbContext context) : base(context)
        {
            _Context = context;
            Requires(b => b.Content);
        }

        protected override Post Construct()
        {
            var content = Get(p => p.Content);

            var instExp = Expression.Parameter(typeof(Post));
            var fieldExp = Expression.Field(instExp, typeof(Post).GetTypeInfo().GetDeclaredField("_Blog"));
            var expr = Expression.Lambda<Func<Post, Blog>>(fieldExp, instExp);
            var blog = Get(expr);

            var post = new Post(blog, content);
            ApplySetters(post);
            _Context.Add(post);
            return post;
        }
    }
}
