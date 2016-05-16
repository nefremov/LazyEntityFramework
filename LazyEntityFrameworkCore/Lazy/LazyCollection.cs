using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using LazyEntityFrameworkCore.ChangeTracking.Internal;
using LazyEntityFrameworkCore.Encapsulation;
using LazyEntityFrameworkCore.Encapsulation.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LazyEntityFrameworkCore.Lazy
{
    [ComVisible(false)]
    public class LazyCollection<T, TOwner> : IEncapsulatedCollection<T>, IQueryable<T> where T : class where TOwner : class
    {
        private readonly Func<TOwner, ICollection<T>> _CollectionAccessor;
        private readonly IQueryable<T> _Query;
        private readonly DbSet<T> _Set;
        private readonly TOwner _Owner;
        private readonly Expression<Func<T, TOwner>> _OwnerMemberExpression;
        private readonly LazyStateManager _StateManager;
        private object _SyncRoot;

        public int Count => _CollectionAccessor(_Owner).Count;
        protected ICollection<T> Items => _CollectionAccessor(_Owner);

        public bool IsReadOnly => _CollectionAccessor(_Owner).IsReadOnly;

        public LazyCollection(DbContext context, INavigation navigation, TOwner owner, Expression<Func<T, bool>> filterExpression)
        {
            //if (collectionAccessor == null)
            //{
            //    throw new ArgumentNullException(nameof(collectionAccessor));
            //}
            _Set = context.Set<T>();
            /*foreach (var property in navigation.ForeignKey.Properties)
            {
                var ann = property.FindAnnotation("BackingField");
                MemberInfo member = null;
                if (ann != null)
                {
                    member =
                        property.DeclaringEntityType.ClrType.GetRuntimeFields()
                            .SingleOrDefault(f => f.Name == ann.Name);
                }
                if (member == null)
                {
                    member =
                        property.DeclaringEntityType.ClrType.GetRuntimeProperties()
                            .SingleOrDefault(p => p.Name == property.Name);
                }
                var
            }*/
            _Query = _Set.Where(filterExpression);
            _Owner = owner;
            var inverse = navigation.FindInverse();
            string fieldName = (string)inverse.ForeignKey.GetAnnotation("InverseField").Value;
            var props =
                navigation.ForeignKey.DeclaringEntityType.ClrType.GetRuntimeFields()
                    .Where(p => p.Name == fieldName)
                    .ToList();
            if (props.Count() > 1)
            {
                throw new AmbiguousMatchException();
            }

            FieldInfo fieldInfo = props.SingleOrDefault();

            _OwnerMemberExpression = CreateGetter<T, TOwner>(fieldInfo);
            //_OwnerMemberExpression = ownerMemberExpression;
            var annotation = navigation.ForeignKey.GetAnnotations().FirstOrDefault(a => a.Name == "BackingField");
                props =
                    navigation.DeclaringEntityType.ClrType.GetRuntimeFields()
                        .Where(p => p.Name == (string)annotation.Value)
                        .ToList();
                if (props.Count() > 1)
                {
                    throw new AmbiguousMatchException();
                }

            fieldInfo = props.SingleOrDefault();
            _CollectionAccessor = CreateGetter<TOwner, ICollection<T>>(fieldInfo).Compile();

            _StateManager = (LazyStateManager)context.GetService<IStateManager>();
        }
        private static Expression<Action<TEntity, TProperty>> CreateSetter<TEntity, TProperty>(FieldInfo field)
        {
            var instExp = Expression.Parameter(typeof(TEntity));
            var fieldExp = Expression.Field(instExp, field);
            var valueExp = Expression.Parameter(typeof(TProperty));
            return Expression.Lambda<Action<TEntity, TProperty>>(Expression.Assign(fieldExp, valueExp), instExp, valueExp);
        }

        private static Expression<Func<TEntity, TProperty>> CreateGetter<TEntity, TProperty>(FieldInfo field)
        {
            var instExp = Expression.Parameter(typeof(TEntity));
            var fieldExp = Expression.Field(instExp, field);
            return Expression.Lambda<Func<TEntity, TProperty>>(fieldExp, instExp);
        }

        private static Expression<Func<TEntity, TProperty>> CreateGetter<TEntity, TProperty>(PropertyInfo property)
        {
            var instExp = Expression.Parameter(typeof(TEntity));
            var fieldExp = Expression.Property(instExp, property);
            return Expression.Lambda<Func<TEntity, TProperty>>(fieldExp, instExp);
        }

        private static Expression<Func<TEntity, TProperty>> CreateGetter<TEntity, TProperty>(MemberInfo member)
        {
            var field = member as FieldInfo;
            if (field != null)
            {
                return CreateGetter<TEntity, TProperty>(field);
            }
            var property = member as PropertyInfo;
            if (property != null)
            {
                return CreateGetter<TEntity, TProperty>(property);
            }
            throw new ArgumentException($"{nameof(member)} must be FieldInfo or ProprtyInfo", nameof(member));
        }


        public bool Contains(T value)
        {
            return _CollectionAccessor(_Owner).Contains(value);
        }

        public void CopyTo(T[] array, int index)
        {
            _CollectionAccessor(_Owner).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Load();
            return _CollectionAccessor(_Owner).GetEnumerator();
        }


        void ICollection<T>.Add(T value)
        {
            //T result = (T)_Set.Add(value);
            //_Collection.Add(result);
            throw new NotSupportedException("Use GetBuilder to create new element attached to this collection");
        }

        void ICollection<T>.Clear()
        {
            _Set.RemoveRange(_CollectionAccessor(_Owner));
            _CollectionAccessor(_Owner).Clear();
        }

        bool ICollection<T>.Remove(T value)
        {
            _Set.Remove(value);
            return _CollectionAccessor(_Owner).Remove(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _CollectionAccessor(_Owner).GetEnumerator();
        }

        public IBuilder<T> GetBuilder()
        {
            IDbContextServices contextServices = _Set.GetInfrastructure().GetService<IDbContextServices>();
            IBuilder<T> builder = _Set.GetInfrastructure().GetService<IBuilderProvider>().GetBuilder<T>(contextServices.CurrentContext.Context);
            if (_OwnerMemberExpression != null)
            {
                builder.Set(_OwnerMemberExpression, _Owner);
            }
            //builder.Built += (b, entity) => _CollectionAccessor(_Owner).Add(entity);
            return builder;
        }

        /*public IBuilder<TDerived> GetBuilder<TDerived>() where TDerived : class, T
        {
            EntityBuilder<TDerived> builder = (EntityBuilder<TDerived>)((IBuilderProvider)_Set).GetBuilder<TDerived>();
            if (_OwnerMemberExpression != null)
            {
                ParameterTypeReplacingExpressionVisitor visitor = new ParameterTypeReplacingExpressionVisitor(typeof(T), typeof(TDerived));

                builder.Set((Expression<Func<TDerived, TOwner>>)visitor.Visit(_OwnerMemberExpression), _Owner);
            }
            return builder;
        }*/


        public IQueryable<T> Query => _Query ?? _CollectionAccessor(_Owner).AsQueryable();

        public bool IsLoaded { get; set; }

        public void Clear()
        {
            Load();
            _CollectionAccessor(_Owner).Clear();
        }

        //public bool Remove(T item)
        //{
        //    //if (!_Collection.Contains(item))
        //    //    Load();
        //    // N.E. I suppose loading is not necessary because if item id in EncapsulatedContext, it is in _Collection already
        //    return _Collection.Remove(item);
        //}

        private void Load()
        {
            if (!IsLoaded && _Query != null && !_StateManager.InTracking)
            {
                _Query.Load();
                IsLoaded = true;
            }
        }
        Expression IQueryable.Expression => (_Query ?? _CollectionAccessor(_Owner).AsQueryable()).Expression;

        Type IQueryable.ElementType => (_Query ?? _CollectionAccessor(_Owner).AsQueryable()).ElementType;

        IQueryProvider IQueryable.Provider => (_Query ?? _CollectionAccessor(_Owner).AsQueryable()).Provider;
    }
}
