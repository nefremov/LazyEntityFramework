using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
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

        private readonly FieldInfo _propertiesFieldInfo = typeof(EntityType).GetTypeInfo().GetDeclaredField("_properties");

        private SortedDictionary<string, Property> _propertiesBase => (SortedDictionary<string, Property>)_propertiesFieldInfo.GetValue(this);


        public MaterializingEntityType(string name, Model model, ConfigurationSource configurationSource) : base(name, model, configurationSource)
        {
        }

        public MaterializingEntityType(Type clrType, Model model, ConfigurationSource configurationSource) : base(clrType, model, configurationSource)
        {
        }

        public object CreateEntity(ValueBuffer valueBuffer)
        {
            DbContext context = (DbContext)valueBuffer[valueBuffer.Count - 1];
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
            //Check.NotEmpty(name, nameof(name));
            //Check.NotNull(foreignKey, nameof(foreignKey));

            return AddNavigation(new PropertyIdentity(name), foreignKey, pointsToPrincipal);
        }
        public override Navigation AddNavigation(
            [NotNull] PropertyInfo navigationProperty,
            [NotNull] ForeignKey foreignKey,
            bool pointsToPrincipal)
        {
            //Check.NotNull(navigationProperty, nameof(navigationProperty));
            //Check.NotNull(foreignKey, nameof(foreignKey));

            return AddNavigation(new PropertyIdentity(navigationProperty), foreignKey, pointsToPrincipal);
        }

        private Navigation AddNavigation(PropertyIdentity propertyIdentity, ForeignKey foreignKey, bool pointsToPrincipal)
        {
            var name = propertyIdentity.Name;
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

            Navigation navigation = null;
            var navigationProperty = propertyIdentity.Property;
            if (ClrType != null)
            {
                Navigation.IsCompatible(
                    propertyIdentity.Name,
                    navigationProperty,
                    this,
                    pointsToPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType,
                    !pointsToPrincipal && !foreignKey.IsUnique,
                    shouldThrow: true);
                navigation = new BackableNavigation(navigationProperty, foreignKey);
            }
            else
            {
                navigation = new BackableNavigation(name, foreignKey);
            }

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


        public override Property AddProperty(
            string name,
            Type propertyType = null,
            bool? shadow = null,
            ConfigurationSource configurationSource = ConfigurationSource.Explicit,
            bool runConventions = true)
        {
            //Check.NotNull(name, nameof(name));

            ValidateCanAddProperty(name);

            if (shadow != true)
            {
                var clrProperty = ClrType?.GetPropertiesInHierarchy(name).FirstOrDefault();
                if (clrProperty != null)
                {
                    if (propertyType != null
                        && propertyType != clrProperty.PropertyType)
                    {
                        throw new InvalidOperationException(CoreStrings.PropertyWrongClrType(
                            name,
                            this.DisplayName(),
                            clrProperty.PropertyType.DisplayName(fullName: false),
                            propertyType.DisplayName(fullName: false)));
                    }

                    return AddProperty(clrProperty, configurationSource, runConventions);
                }

                if (shadow == false)
                {
                    if (ClrType == null)
                    {
                        throw new InvalidOperationException(CoreStrings.ClrPropertyOnShadowEntity(name, this.DisplayName()));
                    }

                    throw new InvalidOperationException(CoreStrings.NoClrProperty(name, this.DisplayName()));
                }
            }

            if (propertyType == null)
            {
                throw new InvalidOperationException(CoreStrings.NoPropertyType(name, this.DisplayName()));
            }
            return AddProperty(new BackableProperty(name, propertyType, this, configurationSource), runConventions);
        }
        public override Property AddProperty(
            PropertyInfo propertyInfo,
            ConfigurationSource configurationSource = ConfigurationSource.Explicit,
            bool runConventions = true)
        {
            //Check.NotNull(propertyInfo, nameof(propertyInfo));

            ValidateCanAddProperty(propertyInfo.Name);

            if (ClrType == null)
            {
                throw new InvalidOperationException(CoreStrings.ClrPropertyOnShadowEntity(propertyInfo.Name, this.DisplayName()));
            }

            if (propertyInfo.DeclaringType == null
                || !propertyInfo.DeclaringType.GetTypeInfo().IsAssignableFrom(ClrType.GetTypeInfo()))
            {
                throw new ArgumentException(CoreStrings.PropertyWrongEntityClrType(
                    propertyInfo.Name, this.DisplayName(), propertyInfo.DeclaringType?.Name));
            }

            return AddProperty(new BackableProperty(propertyInfo, this, configurationSource), runConventions);
        }

        private Property AddProperty(Property property, bool runConventions)
        {
            _propertiesBase.Add(property.Name, property);

            PropertyMetadataChanged();

            if (runConventions)
            {
                property = Model.ConventionDispatcher.OnPropertyAdded(property.Builder)?.Metadata;
            }

            return property;
        }

        private void ValidateCanAddProperty(string name)
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
        }
    }
}