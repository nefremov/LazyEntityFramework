using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class MaterializingModel : Model, IMutableModel
    {
        private readonly SortedDictionary<string, EntityType> _entityTypes = new SortedDictionary<string, EntityType>();

        public MaterializingModel(ConventionSet conventions) : base(conventions)
        {
        }

        public override EntityType AddEntityType(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = AddEntityTypeWithoutConventions(name, configurationSource);
            return ConventionDispatcher.OnEntityTypeAdded(entityType.Builder)?.Metadata;
        }

        public override EntityType AddEntityType(Type type, ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = AddEntityTypeWithoutConventions(type.DisplayName(), configurationSource);
            entityType.ClrType = type;
            return ConventionDispatcher.OnEntityTypeAdded(entityType.Builder)?.Metadata;
        }

        private EntityType AddEntityTypeWithoutConventions(string name, ConfigurationSource configurationSource)
        {
            var entityType = new MaterializingEntityType(name, this, configurationSource);
            var previousLength = _entityTypes.Count;
            _entityTypes[name] = entityType;

            if (previousLength == _entityTypes.Count)
            {
                throw new InvalidOperationException(CoreStrings.DuplicateEntityType(entityType.Name));
            }
            return entityType;
        }

        IMutableEntityType IMutableModel.AddEntityType(string name) => AddEntityType(name);

        public override IEnumerable<EntityType> GetEntityTypes() => _entityTypes.Values;

        public override EntityType FindEntityType(string name)
        {
            EntityType entityType;
            return _entityTypes.TryGetValue(name, out entityType)
                ? entityType
                : null;
        }
        public override EntityType RemoveEntityType(Type type)
        {
            var entityType = FindEntityType(type);
            return entityType == null
                ? null
                : RemoveEntityType(entityType);
        }

        public override EntityType RemoveEntityType(string name)
        {
            var entityType = FindEntityType(name);
            return entityType == null
                ? null
                : RemoveEntityType(entityType);
        }

        private EntityType RemoveEntityType(EntityType entityType)
        {
            var referencingForeignKey = entityType.GetDeclaredReferencingForeignKeys().FirstOrDefault();
            if (referencingForeignKey != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.EntityTypeInUseByForeignKey(
                        entityType.DisplayName(),
                        "{" + string.Join(", ", referencingForeignKey.Properties.Select(p => "'" + p.Name + "'")) + "}",
                        referencingForeignKey.DeclaringEntityType.DisplayName()));
            }

            var derivedEntityType = entityType.GetDirectlyDerivedTypes().FirstOrDefault();
            if (derivedEntityType != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.EntityTypeInUseByDerived(
                        entityType.DisplayName(),
                        derivedEntityType.DisplayName()));
            }

            var removed = _entityTypes.Remove(entityType.Name);
            entityType.Builder = null;

            return entityType;
        }
        IMutableEntityType IMutableModel.RemoveEntityType(string name) => RemoveEntityType(name);
    }
}