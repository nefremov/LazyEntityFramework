using System;
using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class BackableProperty : Property
    {
        private IClrPropertySetter _setter;

        public override IClrPropertySetter Setter
            => LazyInitializer.EnsureInitialized(ref _setter, () => new ClrBackingPropertySetterFactory().Create(this));

        public BackableProperty(string name, Type clrType, EntityType declaringEntityType, ConfigurationSource configurationSource)
            : base(name, clrType, declaringEntityType, configurationSource)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public BackableProperty(PropertyInfo propertyInfo, EntityType declaringEntityType, ConfigurationSource configurationSource)
            : base(propertyInfo, declaringEntityType, configurationSource)
        {
        }
    }
}
