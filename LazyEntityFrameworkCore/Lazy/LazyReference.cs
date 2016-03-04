using System;
using System.Linq;
using System.Linq.Expressions;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyEntityFrameworkCore.Lazy
{
    public class LazyReference<T> where T: class
    {
        private readonly Func<T> _accessor; 
        public T Value => _accessor();

        private bool _loaded = false;
        public LazyReference (DbContext context, Expression<Func<T, bool>> filterExpression, Func<T> accessor)
        {
            var stateManager = (LazyStateManager)context.GetService<IStateManager>();
            var set = context.Set<T>();
            _accessor = () =>
            {
                if (!_loaded && !stateManager.InTracking)
                {
                    set.Where(filterExpression).Load();
                    _loaded = true;
                }
                return accessor();
            };
        }
    }
}
