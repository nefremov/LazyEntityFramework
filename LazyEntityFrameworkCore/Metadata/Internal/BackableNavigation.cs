using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class BackableNavigation : Navigation
    {
        private IClrCollectionAccessor _collectionAccessor;
        private IClrPropertySetter _setter;

        public BackableNavigation(string name, ForeignKey foreignKey) : base(name, foreignKey) { }

        public BackableNavigation(PropertyInfo navigationProperty, ForeignKey foreignKey) : base(navigationProperty, foreignKey) { }

        public override IClrPropertySetter Setter
            => LazyInitializer.EnsureInitialized(ref _setter, () => new ClrBackingPropertySetterFactory().Create(this));

        public override IClrCollectionAccessor CollectionAccessor
            => LazyInitializer.EnsureInitialized(ref _collectionAccessor, () => new BackableClrCollectionAccessorFactory().Create(this));

    }
}
