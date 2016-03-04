using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LazyEntityFrameworkCore.Lazy.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class MaterializingEntityType : EntityType, IEntityMaterializer
    {
        private readonly SortedDictionary<string, Navigation> _navigations
            = new SortedDictionary<string, Navigation>(StringComparer.Ordinal);

        private IProxyBuilder _proxyBuilder;
        public MaterializingEntityType(string name, Model model, ConfigurationSource configurationSource) : base(name, model, configurationSource)
        {
        }

        public object CreateEntity(ValueBuffer valueBuffer)
        {
            DbContext context = (DbContext) valueBuffer[valueBuffer.Count - 1];
            if (_proxyBuilder == null)
            {
                _proxyBuilder = context.GetInfrastructure().GetService<IProxyBuilder>();
            }

            return _proxyBuilder.ConstructProxy(this, valueBuffer, context);
        }
        internal static string Format(IEnumerable<IProperty> properties)
        {
            return "{" + string.Join(", ", properties.Select(p => "'" + p.Name + "'")) + "}";
        }
        public override Navigation AddNavigation(
            string name,
            ForeignKey foreignKey,
            bool pointsToPrincipal)
        {

            var duplicateNavigation = FindNavigationsInHierarchy(name).FirstOrDefault();
            if (duplicateNavigation != null)
            {
                if (duplicateNavigation.ForeignKey != foreignKey)
                {
                    throw new InvalidOperationException(
                        CoreStrings.NavigationForWrongForeignKey(
                            duplicateNavigation.Name,
                            duplicateNavigation.DeclaringEntityType.DisplayName(),
                            Format(foreignKey.Properties),
                            Format(duplicateNavigation.ForeignKey.Properties)));
                }

                throw new InvalidOperationException(
                    CoreStrings.DuplicateNavigation(name, this.DisplayName(), duplicateNavigation.DeclaringEntityType.DisplayName()));
            }

            var duplicateProperty = FindPropertiesInHierarchy(name).FirstOrDefault();
            if (duplicateProperty != null)
            {
                throw new InvalidOperationException(CoreStrings.ConflictingProperty(name, this.DisplayName(),
                    duplicateProperty.DeclaringEntityType.DisplayName()));
            }

            Debug.Assert(!GetNavigations().Any(n => (n.ForeignKey == foreignKey) && (n.IsDependentToPrincipal() == pointsToPrincipal)),
                "There is another navigation corresponding to the same foreign key and pointing in the same direction.");

            Debug.Assert((pointsToPrincipal ? foreignKey.DeclaringEntityType : foreignKey.PrincipalEntityType) == this,
                "EntityType mismatch");

            Navigation.IsCompatible(
                name,
                this,
                pointsToPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType,
                !pointsToPrincipal && !foreignKey.IsUnique,
                shouldThrow: true);

            var navigation = new BackableNavigation(name, foreignKey);
            _navigations.Add(name, navigation);

            PropertyMetadataChanged();

            return navigation;
        }
        public override Navigation FindDeclaredNavigation(string name)
        {
            Navigation navigation;
            return _navigations.TryGetValue(name, out navigation)
                ? navigation
                : null;
        }

        public override IEnumerable<Navigation> GetDeclaredNavigations() => _navigations.Values;
        public override Navigation RemoveNavigation(string name)
        {
            var navigation = FindDeclaredNavigation(name);
            if (navigation == null)
            {
                return null;
            }

            _navigations.Remove(name);

            PropertyMetadataChanged();

            return navigation;
        }

        public override IEnumerable<Navigation> GetNavigations()
            => BaseType?.GetNavigations().Concat(_navigations.Values) ?? _navigations.Values;


        private readonly FieldInfo _propertiesFieldInfo = typeof (EntityType).GetTypeInfo().GetDeclaredField("_properties");

        private SortedDictionary<string, Property> _propertiesBase => (SortedDictionary<string, Property>)_propertiesFieldInfo.GetValue(this);

        public override Property AddProperty(
    string name,
    ConfigurationSource configurationSource = ConfigurationSource.Explicit,
    bool runConventions = true)
    {
        var duplicateProperty = FindPropertiesInHierarchy(name).FirstOrDefault();
        if (duplicateProperty != null)
        {
            throw new InvalidOperationException(CoreStrings.DuplicateProperty(
                name, this.DisplayName(), duplicateProperty.DeclaringEntityType.DisplayName()));
        }

        var duplicateNavigation = FindNavigationsInHierarchy(name).FirstOrDefault();
        if (duplicateNavigation != null)
        {
            throw new InvalidOperationException(CoreStrings.ConflictingNavigation(name, this.DisplayName(),
                duplicateNavigation.DeclaringEntityType.DisplayName()));
        }

        var property = new BackableProperty(name, this, configurationSource);

        _propertiesBase.Add(name, property);

        PropertyMetadataChanged();

        if (runConventions)
        {
            property = (BackableProperty)Model.ConventionDispatcher.OnPropertyAdded(property.Builder)?.Metadata;
        }

        return property;
    }
    }
}