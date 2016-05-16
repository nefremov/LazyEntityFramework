using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Metadata.Internal
{
    public class MaterializingModel : Model, IMutableModel
    {
        private readonly SortedDictionary<string, EntityType> _entityTypes
            = new SortedDictionary<string, EntityType>();

        private readonly IDictionary<Type, EntityType> _clrTypeMap
            = new Dictionary<Type, EntityType>();

        public MaterializingModel(ConventionSet conventions) : base(conventions)
        {
        }

        public override IEnumerable<EntityType> GetEntityTypes() => _entityTypes.Values;

        public override EntityType AddEntityType([NotNull] string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = new MaterializingEntityType(name, this, configurationSource);

            return AddEntityType(entityType);
        }

        public override EntityType AddEntityType([NotNull] Type type, ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = new MaterializingEntityType(type, this, configurationSource);

            _clrTypeMap[type] = entityType;
            return AddEntityType(entityType);
        }

        private EntityType AddEntityType(EntityType entityType)
        {
            var previousLength = _entityTypes.Count;
            _entityTypes[entityType.Name] = entityType;
            if (previousLength == _entityTypes.Count)
            {
                throw new InvalidOperationException(CoreStrings.DuplicateEntityType(entityType.Name));
            }

            return ConventionDispatcher.OnEntityTypeAdded(entityType.Builder)?.Metadata;
        }

        public override EntityType GetOrAddEntityType([NotNull] Type type)
            => FindConcreteEntityType(type) ?? AddEntityType(type);


        public override EntityType FindEntityType(Type type)
        {
            var entityType = FindConcreteEntityType(type);
            type = type.GetTypeInfo().BaseType;
            while (entityType == null && type != null)
            {
                entityType = FindConcreteEntityType(type);
                type = type.GetTypeInfo().BaseType;
            }
            return entityType;
        }
        public override EntityType FindEntityType([NotNull] string name)
        {
            EntityType entityType;
            return _entityTypes.TryGetValue(name, out entityType)
                ? entityType
                : null;
        }

        public EntityType FindConcreteEntityType([NotNull] Type type)
        {
            EntityType entityType;
            return _clrTypeMap.TryGetValue(type, out entityType)
                ? entityType
                : FindEntityType(type.DisplayName());
        }

        public override EntityType RemoveEntityType([NotNull] Type type)
        {
            var entityType = FindEntityType(type);
            return entityType == null
                ? null
                : RemoveEntityType(entityType);
        }

        public override EntityType RemoveEntityType([NotNull] string name)
        {
            var entityType = FindEntityType(name);
            return entityType == null
                ? null
                : RemoveEntityType(entityType);
        }

        private EntityType RemoveEntityType([NotNull] EntityType entityType)
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

            if (entityType.ClrType != null)
            {
                _clrTypeMap.Remove(entityType.ClrType);
            }

            var removed = _entityTypes.Remove(entityType.Name);
            Debug.Assert(removed);
            entityType.Builder = null;

            return entityType;
        }

        IEntityType IModel.FindEntityType(string name) => FindEntityType(name);
        IEnumerable<IEntityType> IModel.GetEntityTypes() => GetEntityTypes();

        IMutableEntityType IMutableModel.AddEntityType(string name) => AddEntityType(name);
        IMutableEntityType IMutableModel.AddEntityType(Type type) => AddEntityType(type);
        IEnumerable<IMutableEntityType> IMutableModel.GetEntityTypes() => GetEntityTypes();
        IMutableEntityType IMutableModel.FindEntityType(string name) => FindEntityType(name);
        IMutableEntityType IMutableModel.RemoveEntityType(string name) => RemoveEntityType(name);
    }
}