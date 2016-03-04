using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class ClrBackingPropertySetterFactory : ClrPropertySetterFactory
    {
        public override IClrPropertySetter Create(IPropertyBase property)
        {
            var setter = property as IClrPropertySetter;
            if (setter != null)
            {
                return setter;
            }

            var navigation = property as Navigation;
            if (navigation != null)
            {
                var field = (string) navigation.ForeignKey["InverseField"];
                if (field != null)
                {
                    var fieldInfo =
                        property.DeclaringEntityType.ClrType.GetRuntimeFields().SingleOrDefault(f => f.Name == field);
                    if (fieldInfo != null)
                    {
                        return Create(fieldInfo);
                    }
                }
                return base.Create(property);
            }

            var fieldName = (string)property["BackingField"];
            if (fieldName != null)
            {
                var fieldInfo =
                    property.DeclaringEntityType.ClrType.GetRuntimeFields().SingleOrDefault(f => f.Name == fieldName);
                if (fieldInfo != null)
                {
                    return Create(fieldInfo);
                }
            }
            return base.Create(property);
        }


        public static Type UnwrapNullableType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        public static bool IsNullableType(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return !typeInfo.IsValueType
                   || (typeInfo.IsGenericType
                       && (typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }
        public IClrPropertySetter Create(FieldInfo field)
        {
            // TODO: Handle case where there is not setter or setter is private on a base type
            // Issue #753

            var types = new[] { field.DeclaringType, field.FieldType };

            return (IClrPropertySetter)Activator.CreateInstance(
                IsNullableType(field.FieldType)
                && UnwrapNullableType(field.FieldType).GetTypeInfo().IsEnum
                    ? typeof(NullableEnumClrPropertySetter<,,>).MakeGenericType(
                        field.DeclaringType, field.FieldType, UnwrapNullableType(field.FieldType))
                    : typeof(ClrPropertySetter<,>).MakeGenericType(types),
                _genericCreateSetter.MakeGenericMethod(types).Invoke(null, new object[] {field}));
        }

        private static readonly MethodInfo _genericCreateSetter
            = typeof(ClrBackingPropertySetterFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateSetter));


        public static Action<T, TProperty> CreateSetter<T, TProperty>(FieldInfo field)
        {
            var instExp = Expression.Parameter(typeof(T));
            var fieldExp = Expression.Field(instExp, field);
                var valueExp = Expression.Parameter(typeof(TProperty));
                return Expression.Lambda<Action<T, TProperty>>(Expression.Assign(fieldExp, valueExp), instExp, valueExp).Compile();
        } 

    }
}
