using System;
using System.Linq.Expressions;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public class PropertyAssignment<T, TProperty> : IActionHost<T>
    {
        public TProperty Value { get; set; }
        public Expression<Action<T>> Expression { get; set; }
    }
}