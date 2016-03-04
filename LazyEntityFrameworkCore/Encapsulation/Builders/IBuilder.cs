using System;
using System.Linq.Expressions;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public interface IBuilder<T>
    {
        T Build();
        IBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> expression, TProperty value);

        event Action<IBuilder<T>, T> Built;
        IBuilder<T> Clone();
    }
}
