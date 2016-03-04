using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LazyEntityFrameworkCore.Lazy.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public abstract class EntityBuilder<T> : IBuilder<T> where T : class
    {
        private static readonly IEqualityComparer<Expression> _Comparer = new Microsoft.EntityFrameworkCore.Query.Internal.ExpressionEqualityComparer();
        private readonly DbContext _Context;
        private Dictionary<LambdaExpression, IActionHost<T>> _Setters = new Dictionary<LambdaExpression, IActionHost<T>>(_Comparer);
        private HashSet<LambdaExpression> _Ignored = new HashSet<LambdaExpression>(_Comparer);
        private Dictionary<string, bool> _Required = new Dictionary<string, bool>();
        private DbContext _context;

        protected EntityBuilder(DbContext context)
        {
            _Context = context;
        }

        /// Use to define required properties of T in implementors of EntityBuilder<typeparam name="T"></typeparam>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="expression">member expression that defines required property of T</param>
        protected EntityBuilder<T> Requires<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MemberExpression body = expression.Body as MemberExpression;
            if (body == null)
            {
                return this;
            }

            if (!_Required.ContainsKey(body.Member.Name))
            {
                _Required.Add(body.Member.Name, false);
            }
            return this;
        }

        /// <summary>
        /// Use to define properties of T to be ignored in implementors of EntityBuilder<typeparam name="T"></typeparam>
        /// </summary>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="expression">member expression that defines property of T to be ignored</param>
        protected EntityBuilder<T> Ignores<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MemberExpression body = expression.Body as MemberExpression;
            if (body == null)
            {
                return this;
            }

            _Ignored.Add(expression);
            return this;
        }

        /// <summary>
        /// Customers use this method to assign values to properties of instance of T being constructed. Adds expression and value to internal setter list.
        /// </summary>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="expression">member expression that defines property of T to be assigned</param>
        /// <param name="value">value to be assigned</param>
        /// <returns></returns>
        public IBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> expression, TProperty value)
        {
            MemberExpression body = expression.Body as MemberExpression;
            Expression<Func<T, TProperty>> local = expression;
            if (body == null)
            {
                UnaryExpression convert = expression.Body as UnaryExpression;
                if (convert == null || convert.NodeType != ExpressionType.Convert)
                {
                    return this;
                }
                body = convert.Operand as MemberExpression;
                if (body == null)
                {
                    return this;
                }
                local = (Expression<Func<T, TProperty>>)Expression.Lambda(Expression.MakeMemberAccess(expression.Parameters[0], body.Member), expression.Parameters);
            }

            if (_Ignored.Contains(expression))
            {
                return this;
            }

            PropertyAssignment<T, TProperty> assignment = new PropertyAssignment<T, TProperty>
            {
                Expression = Expression.Lambda<Action<T>>(Expression.Assign(local.Body, Expression.Constant(value, typeof(TProperty))), local.Parameters),
                Value = value
            };
            _Setters.Add(local, assignment);
            if (_Required.ContainsKey(body.Member.Name))
            {
                _Required[body.Member.Name] = true;
            }
            return this;
        }

        protected TProperty Get<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MemberExpression body = expression.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentException("Expression must be member access expression");
            }
            IActionHost<T> result;
            if (!_Setters.TryGetValue(expression, out result))
            {
                throw new ArgumentException(string.Format("Value for '{0}' has not been provided yet", body.Member.Name));
            }

            return ((PropertyAssignment<T, TProperty>)result).Value;
        }

        /// <summary>
        /// During construction process use this method to take desired property value and remove expression from setter list because this value will be passed to constructor of T and would not be explicitely assigned by property setter
        /// </summary>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="expression">member expression that defines property of T to be removed from setter list</param>
        /// <returns>value requested to be assigned to this property</returns>
        protected TProperty Remove<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            MemberExpression body = expression.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentException("Expression must be member access expression");
            }
            IActionHost<T> result;
            if (!_Setters.TryGetValue(expression, out result))
            {
                throw new ArgumentException(string.Format("Value for '{0}' has not been provided yet", body.Member.Name));
            }

            TProperty value = ((PropertyAssignment<T, TProperty>)result).Value;
            _Setters.Remove(expression);
            return value;
        }

        /// <summary>
        /// Safe version or Remove. Does not throw exceptions.
        /// </summary>
        /// <typeparam name="TProperty">Property type</typeparam>
        /// <param name="expression">member expression that defines property of T to be removed from setter list</param>
        /// <param name="result">value requested to be assigned to this property or default of TProperty if expression is not found in setter list</param>
        /// <returns>true, when expression is found in setter list, otherwise false</returns>
        protected bool TryRemove<TProperty>(Expression<Func<T, TProperty>> expression, out TProperty result)
        {
            MemberExpression body = expression.Body as MemberExpression;
            if (body == null)
            {
                result = default(TProperty);
                return false;
            }
            IActionHost<T> setter;
            if (!_Setters.TryGetValue(expression, out setter))
            {
                result = default(TProperty);
                return false;
            }

            TProperty value = ((PropertyAssignment<T, TProperty>)setter).Value;
            _Setters.Remove(expression);
            result = value;
            return true;
        }

        /// <summary>
        /// Validate that customer provided values for all required properties
        /// </summary>
        /// <returns>true, if customer provided values for all required properties</returns>
        protected bool ValidateRequired()
        {
            return !_Required.ContainsValue(false);
        }

        /// <summary>
        /// Apply property values provided by customer
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected T ApplySetters(T entity)
        {
            foreach (KeyValuePair<LambdaExpression, IActionHost<T>> setter in _Setters)
            {
                setter.Value.Expression.Compile()(entity);
            }
            return entity;
        }

        /// <summary>
        /// Implement in inheritors to build instance of T.
        /// </summary>
        /// <returns></returns>
        protected virtual T Construct()
        {
            return (T)_Context.GetService<IProxyBuilder>().ConstructProxy(_Context.Model.FindEntityType(typeof(T)), _Context);
        }


        /// <summary>
        /// Final method to build fully initialized instance of T
        /// </summary>
        /// <returns></returns>
        public T Build()
        {
            if (_Required.ContainsValue(false))
            {
                throw new InvalidOperationException("Values has not been provided yet for some required properties");
            }

            T entity = Construct();

            ApplySetters(entity);

            OnBuilt(entity);
            return entity;
        }

        public event Action<IBuilder<T>, T> Built;

        public void OnBuilt(T entity)
        {
            Built?.Invoke(this, entity);
        }

        /// <summary>
        /// Clone builder. Internal dictionaries and collections will be cloned, their members will not. 
        /// </summary>
        /// <returns></returns>
        public IBuilder<T> Clone()
        {
            EntityBuilder<T> clone = (EntityBuilder<T>)MemberwiseClone();
            clone._Ignored = new HashSet<LambdaExpression>(_Ignored, _Comparer);
            clone._Setters = new Dictionary<LambdaExpression, IActionHost<T>>(_Setters, _Comparer);
            clone._Required = new Dictionary<string, bool>(_Required);
            return clone;
        }
    }
}
