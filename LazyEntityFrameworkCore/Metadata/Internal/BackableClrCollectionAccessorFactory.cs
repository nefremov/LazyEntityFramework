using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class BackableClrCollectionAccessorFactory : ClrCollectionAccessorFactory
    {
        private static readonly MethodInfo _genericCreate
            = typeof(BackableClrCollectionAccessorFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateGeneric));

        private static readonly MethodInfo _genericCreateField
            = typeof(BackableClrCollectionAccessorFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateGenericField));

        private static readonly MethodInfo _createAndSet
            = typeof(ClrCollectionAccessorFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateAndSet));

        private static readonly MethodInfo _create
            = typeof(ClrCollectionAccessorFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateCollection));

        public static IEnumerable<Type> GetBaseTypes(Type type)
        {
            type = type.GetTypeInfo().BaseType;

            while (type != null)
            {
                yield return type;

                type = type.GetTypeInfo().BaseType;
            }
        }

        public static IEnumerable<Type> GetGenericTypeImplementations(Type type, Type interfaceOrBaseType)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericTypeDefinition)
            {
                return (interfaceOrBaseType.GetTypeInfo().IsInterface ? typeInfo.ImplementedInterfaces : GetBaseTypes(type))
                    .Union(new[] { type })
                    .Where(
                        t => t.GetTypeInfo().IsGenericType
                             && (t.GetGenericTypeDefinition() == interfaceOrBaseType));
            }

            return Enumerable.Empty<Type>();
        }
        public static Type TryGetElementType(Type type, Type interfaceOrBaseType)
        {
            if (!type.GetTypeInfo().IsGenericTypeDefinition)
            {
                var types = GetGenericTypeImplementations(type, interfaceOrBaseType).ToArray();

                return types.Length == 1 ? types[0].GetTypeInfo().GenericTypeArguments.FirstOrDefault() : null;
            }

            return null;
        }
        public static IEntityType GetTargetType(INavigation navigation)
        {
            return navigation.ForeignKey.DependentToPrincipal == navigation
                ? navigation.ForeignKey.PrincipalEntityType
                : navigation.ForeignKey.DeclaringEntityType;
        }
        public override IClrCollectionAccessor Create(INavigation navigation)
        {
            var accessor = navigation as IClrCollectionAccessor;

            if (accessor != null)
            {
                return accessor;
            }

            var annotation = navigation.ForeignKey.GetAnnotations().FirstOrDefault(a => a.Name == "BackingField");
            FieldInfo memberInfo = null;
            PropertyInfo propertyInfo = null;
            if (annotation != null)
            {
                var props =
                    navigation.DeclaringEntityType.ClrType.GetRuntimeFields()
                        .Where(p => p.Name == (string)annotation.Value)
                        .ToList();
                if (props.Count() > 1)
                {
                    throw new AmbiguousMatchException();
                }

                memberInfo = props.SingleOrDefault();
            }
            if (memberInfo == null)
            {
                var props =
                    navigation.DeclaringEntityType.ClrType.GetRuntimeProperties()
                        .Where(p => p.Name == navigation.Name)
                        .ToList();
                if (props.Count() > 1)
                {
                    throw new AmbiguousMatchException();
                }

                propertyInfo = props.SingleOrDefault();
            }
            var elementType = TryGetElementType(memberInfo?.FieldType ?? propertyInfo.PropertyType, typeof(ICollection<>));

            // TODO: Only ICollections supported; add support for enumerables with add/remove methods
            // Issue #752
            if (elementType == null)
            {
                throw new NotSupportedException(
                    CoreStrings.NavigationBadType(
                        navigation.Name, navigation.DeclaringEntityType.Name, (memberInfo?.FieldType ?? propertyInfo.PropertyType).FullName, GetTargetType(navigation).Name));
            }

            if ((memberInfo?.FieldType ?? propertyInfo.PropertyType).IsArray)
            {
                throw new NotSupportedException(
                    CoreStrings.NavigationArray(navigation.Name, navigation.DeclaringEntityType.Name, (memberInfo?.FieldType ?? propertyInfo.PropertyType).FullName));
            }

            if (propertyInfo != null && propertyInfo.GetMethod == null)
            {
                throw new NotSupportedException(CoreStrings.NavigationNoGetter(navigation.Name, navigation.DeclaringEntityType.Name));
            }

            if (memberInfo != null)
            {
                var boundMethod = _genericCreateField.MakeGenericMethod(
                    navigation.DeclaringEntityType.ClrType, memberInfo.FieldType, elementType);

                return (IClrCollectionAccessor) boundMethod.Invoke(null, new object[] {memberInfo});
            }
            else
            {
                var boundMethod =  _genericCreate.MakeGenericMethod(
                    navigation.DeclaringEntityType.ClrType, propertyInfo.PropertyType, elementType);

                return (IClrCollectionAccessor) boundMethod.Invoke(null, new object[] {propertyInfo});
            }
        }

        // ReSharper disable once UnusedMember.Local
        private static IClrCollectionAccessor CreateGeneric<TEntity, TCollection, TElement>(PropertyInfo property)
            where TEntity : class
            where TCollection : class, ICollection<TElement>
        {
            var getterDelegate = (Func<TEntity, TCollection>)property.GetMethod.CreateDelegate(typeof(Func<TEntity, TCollection>));

            Action<TEntity, TCollection> setterDelegate = null;
            Func<TEntity, Action<TEntity, TCollection>, TCollection> createAndSetDelegate = null;
            Func<TCollection> createDelegate = null;

            var setter = property.SetMethod;
            if (setter != null)
            {
                setterDelegate = (Action<TEntity, TCollection>)setter.CreateDelegate(typeof(Action<TEntity, TCollection>));

                var concreteType = new CollectionTypeFactory().TryFindTypeToInstantiate(typeof(TEntity), typeof(TCollection));

                if (concreteType != null)
                {
                    createAndSetDelegate = (Func<TEntity, Action<TEntity, TCollection>, TCollection>)_createAndSet
                        .MakeGenericMethod(typeof(TEntity), typeof(TCollection), concreteType)
                        .CreateDelegate(typeof(Func<TEntity, Action<TEntity, TCollection>, TCollection>));

                    createDelegate = (Func<TCollection>)_create
                        .MakeGenericMethod(typeof(TCollection), concreteType)
                        .CreateDelegate(typeof(Func<TCollection>));
                }
            }

            return new ClrICollectionAccessor<TEntity, TCollection, TElement>(
                property.Name, getterDelegate, setterDelegate, createAndSetDelegate, createDelegate);
        }

        public static Func<T, TProperty> CreateGetter<T, TProperty>(FieldInfo field)
        {
            var instExp = Expression.Parameter(typeof(T));
            var fieldExp = Expression.Field(instExp, field);
            return Expression.Lambda<Func<T, TProperty>>(fieldExp, instExp).Compile();
         } 

        public static Action<T, TProperty> CreateSetter<T, TProperty>(FieldInfo field)
        {
            var instExp = Expression.Parameter(typeof(T));
            var fieldExp = Expression.Field(instExp, field);
                var valueExp = Expression.Parameter(typeof(TProperty));
                return Expression.Lambda<Action<T, TProperty>>(Expression.Assign(fieldExp, valueExp), instExp, valueExp).Compile();
        } 

         private static IClrCollectionAccessor CreateGenericField<TEntity, TCollection, TElement>(FieldInfo field)
            where TEntity : class
            where TCollection : class, ICollection<TElement>
        {
            var getterDelegate = CreateGetter<TEntity, TCollection>(field);

            Func<TEntity, Action<TEntity, TCollection>, TCollection> createAndSetDelegate = null;
            Func<TCollection> createDelegate = null;

            var  setterDelegate = CreateSetter<TEntity, TCollection>(field);

                var concreteType = new CollectionTypeFactory().TryFindTypeToInstantiate(typeof(TEntity), typeof(TCollection));

                if (concreteType != null)
                {
                    createAndSetDelegate = (Func<TEntity, Action<TEntity, TCollection>, TCollection>)_createAndSet
                        .MakeGenericMethod(typeof(TEntity), typeof(TCollection), concreteType)
                        .CreateDelegate(typeof(Func<TEntity, Action<TEntity, TCollection>, TCollection>));

                    createDelegate = (Func<TCollection>)_create
                        .MakeGenericMethod(typeof(TCollection), concreteType)
                        .CreateDelegate(typeof(Func<TCollection>));
                }
 
            return new ClrICollectionAccessor<TEntity, TCollection, TElement>(
                field.Name, getterDelegate, setterDelegate, createAndSetDelegate, createDelegate);
        }

       // ReSharper disable once UnusedMember.Local
        private static TCollection CreateAndSet<TEntity, TCollection, TConcreteCollection>(
            TEntity entity,
            Action<TEntity, TCollection> setterDelegate)
            where TEntity : class
            where TCollection : class
            where TConcreteCollection : TCollection, new()
        {
            var collection = new TConcreteCollection();
            setterDelegate(entity, collection);
            return collection;
        }

        // ReSharper disable once UnusedMember.Local
        private static TCollection CreateCollection<TCollection, TConcreteCollection>()
            where TCollection : class
            where TConcreteCollection : TCollection, new()
        {
            return new TConcreteCollection();
        }
    }
}
