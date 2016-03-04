using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public class BuilderProvider : IBuilderProvider
    {
        private Dictionary<Type, Type> _Map = new Dictionary<Type, Type>(); 

        public IBuilderProvider Register<T, TBuilder>() where T : class where TBuilder : IBuilder<T>
        {
            _Map[typeof(T)] = typeof(TBuilder);
            return this;
        }

        public IBuilder<T> GetBuilder<T>(DbContext context) where T : class
        {
            Type builderType;
            if (_Map.TryGetValue(typeof(T), out builderType))
            {
                return (IBuilder<T>)builderType.GetConstructor(new Type[]{typeof(DbContext) }).Invoke(new object[] { context});
            }
            return null;
        }
    }
}
