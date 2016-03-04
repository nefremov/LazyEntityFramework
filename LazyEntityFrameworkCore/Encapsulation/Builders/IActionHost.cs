using System;
using System.Linq.Expressions;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public interface IActionHost<T>
    {
        Expression<Action<T>> Expression { get; }
    }
}