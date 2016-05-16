using System;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class BackableProperty : Property
    {
        private IClrPropertySetter _setter;

        public override IClrPropertySetter Setter
            => LazyInitializer.EnsureInitialized(ref _setter, () => new ClrBackingPropertySetterFactory().Create(this));

        public BackableProperty(
            [NotNull] string name,
            [NotNull] Type clrType,
            [NotNull] EntityType declaringEntityType,
            ConfigurationSource configurationSource)
            : base(name, clrType, declaringEntityType, configurationSource)
        {
        }

        public BackableProperty(
            [NotNull] PropertyInfo propertyInfo,
            [NotNull] EntityType declaringEntityType,
            ConfigurationSource configurationSource)
            : base(propertyInfo, declaringEntityType, configurationSource)
        {
        }
    }
}
