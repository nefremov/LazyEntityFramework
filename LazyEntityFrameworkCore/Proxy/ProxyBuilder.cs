using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LazyEntityFrameworkCore.Proxy
{
    public class ProxyBuilder : IProxyBuilder
    {
        private readonly IMemberMapper _memberMapper;
        private readonly Dictionary<Type, Type> _map = new Dictionary<Type, Type>(); 

        private ConcurrentDictionary<EntityType, Func<ValueBuffer, DbContext, object>> _builders = new ConcurrentDictionary<EntityType, Func<ValueBuffer, DbContext, object>>();

        public ProxyBuilder(IMemberMapper memberMapper)
        {
            _memberMapper = memberMapper;
        }

        public Type GetProxy(Type entityType)
        {
            Type proxyType;
            return _map.TryGetValue(entityType, out proxyType) ? proxyType : entityType;
        }

        public IProxyBuilder RegisterProxy(Type entityType, Type proxyType)
        {
            _map[entityType] = proxyType;
            return this;
        }

        public IProxyBuilder RegisterProxy<TEntity, TProxy>()
        {
            _map[typeof(TEntity)] = typeof(TProxy);
            return this;
        }

        public object ConstructProxy(EntityType entityType, ValueBuffer valueBuffer, DbContext context)
        {
            Func<ValueBuffer, DbContext, object> builder = _builders.GetOrAdd(entityType, CreateBuilder);
            return builder(valueBuffer, context);
        }

        private Func<ValueBuffer, DbContext, object> CreateBuilder(EntityType entityType)
        {
            //IMemberMapper memberMapper = context.GetInfrastructure().GetService<IMemberMapper>();

            var valueBufferExpession = Expression.Parameter(typeof(ValueBuffer), "valueBuffer");
            var contextExpession = Expression.Parameter(typeof(DbContext), "context");

            var proxyType = _map[entityType.ClrType];
            var instanceVariable = Expression.Variable(proxyType, "instance");

            var blockExpressions
                = new List<Expression>
                {
                    Expression.Assign(
                        instanceVariable,
                        Expression.New(GetDeclaredConstructor(proxyType, new [] {typeof(DbContext)}), contextExpession))
                };

            blockExpressions.AddRange(
                from mapping in _memberMapper.MapPropertiesToMembers(entityType)
                let propertyInfo = mapping.Item2 as PropertyInfo
                let targetMember
                    = propertyInfo != null
                        ? Expression.Property(instanceVariable, propertyInfo)
                        : Expression.Field(instanceVariable, (FieldInfo)mapping.Item2)
                select
                    Expression.Assign(
                        targetMember,
                        CreateReadValueExpression(
                            valueBufferExpession,
                            targetMember.Type,
                            mapping.Item1.GetIndex())));

            blockExpressions.Add(instanceVariable);

            return Expression.Lambda<Func<ValueBuffer, DbContext, object>>(Expression.Block(new[] { instanceVariable }, blockExpressions), valueBufferExpession, contextExpession).Compile();
        }
        public static ConstructorInfo GetDeclaredConstructor(Type type, Type[] types)
        {
            types = types ?? new Type[0];

            return type.GetTypeInfo().DeclaredConstructors
                .SingleOrDefault(
                    c => !c.IsStatic
                         && c.GetParameters().Select(p => p.ParameterType).SequenceEqual(types));
        }

        private static readonly MethodInfo _readValue
            = typeof(ValueBuffer).GetTypeInfo().DeclaredProperties
                .Single(p => p.GetIndexParameters().Any()).GetMethod;
        public virtual Expression CreateReadValueExpression(Expression valueBuffer, Type type, int index)
           => Expression.Convert(CreateReadValueCallExpression(valueBuffer, index), type);

        public virtual Expression CreateReadValueCallExpression(Expression valueBuffer, int index)
            => Expression.Call(valueBuffer, _readValue, Expression.Constant(index));
    }
}