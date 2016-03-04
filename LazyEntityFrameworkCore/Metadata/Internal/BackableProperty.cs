using System.Threading;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class BackableProperty : Property
    {
        private IClrPropertySetter _setter;

        public override IClrPropertySetter Setter
            => LazyInitializer.EnsureInitialized(ref _setter, () => new ClrBackingPropertySetterFactory().Create(this));

        public BackableProperty(string name, EntityType declaringEntityType, ConfigurationSource configurationSource)
            : base(name, declaringEntityType, configurationSource)
        {
        }
    }
}
