using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.ChangeTracking.Internal
{
    public class LazyStateManager : StateManager
    {
        private readonly Dictionary<object, InternalEntityEntry> _entityReferenceMap
            = new Dictionary<object, InternalEntityEntry>(ReferenceEqualityComparer.Instance);

        private readonly LazyRef<IDictionary<object, IList<Tuple<INavigation, InternalEntityEntry>>>> _referencedUntrackedEntities
            = new LazyRef<IDictionary<object, IList<Tuple<INavigation, InternalEntityEntry>>>>(
                () => new Dictionary<object, IList<Tuple<INavigation, InternalEntityEntry>>>());

        private IIdentityMap _identityMap0;
        private IIdentityMap _identityMap1;
        private Dictionary<IKey, IIdentityMap> _identityMaps;

        private readonly IInternalEntityEntryFactory _factory;
        private readonly IInternalEntityEntrySubscriber _subscriber;
        private readonly IModel _model;


        public LazyStateManager(
            IInternalEntityEntryFactory factory,
            IInternalEntityEntrySubscriber subscriber, 
            IInternalEntityEntryNotifier notifier,
            IValueGenerationManager valueGeneration,
            IModel model, 
            IDatabase database,
            IConcurrencyDetector concurrencyDetector,
            ICurrentDbContext context) 
            : base(factory, subscriber, notifier, valueGeneration, model, database, concurrencyDetector, context)
        {
            _factory = factory;
            _subscriber = subscriber;
            _model = model;
        }

        public override InternalEntityEntry GetOrCreateEntry(object entity)
        {
            var entry = TryGetEntry(entity);
            if (entry == null)
            {
                SingleQueryMode = false;

                var clrType = entity.GetType();
                var entityType = _model.FindEntityType(clrType);
                clrType = clrType.GetTypeInfo().BaseType;
                while (entityType == null && clrType != null)
                {
                    entityType = _model.FindEntityType(clrType);
                    clrType = clrType.GetTypeInfo().BaseType;
                }

                if (entityType == null)
                {
                    throw new InvalidOperationException(CoreStrings.EntityTypeNotFound(entity.GetType().DisplayName(false)));
                }

                entry = _factory.Create(this, entityType, entity);

                _entityReferenceMap[entity] = entry;
            }
            return entry;
        }
        public bool InTracking { get; set; }
        public override InternalEntityEntry StartTrackingFromQuery(IEntityType baseEntityType, object entity, ValueBuffer valueBuffer)
        {
            InTracking = true;
            try
            {
                var existingEntry = TryGetEntry(entity);
                if (existingEntry != null)
                {
                    return existingEntry;
                }

                var clrType = entity.GetType();

                var newEntry = _factory.Create(this,
                    baseEntityType.ClrType == clrType
                        ? baseEntityType
                        : _model.FindEntityType(clrType),
                    entity, valueBuffer);

                _subscriber.SnapshotAndSubscribe(newEntry);

                foreach (var key in baseEntityType.GetKeys())
                {
                    GetOrCreateIdentityMap(key).AddOrUpdate(newEntry);
                }

                _entityReferenceMap[entity] = newEntry;

                newEntry.MarkUnchangedFromQuery();

                return newEntry;
            }
            finally
            {
                InTracking = false;
            }
        }
        public override InternalEntityEntry TryGetEntry(IKey key, ValueBuffer valueBuffer, bool throwOnNullKey)
            => GetOrCreateIdentityMap(key).TryGetEntry(valueBuffer, throwOnNullKey);

        public override InternalEntityEntry TryGetEntry(object entity)
        {
            InternalEntityEntry entry;
            return !_entityReferenceMap.TryGetValue(entity, out entry) ? null : entry;
        }
        private IIdentityMap GetOrCreateIdentityMap(IKey key)
        {
            if (_identityMap0 == null)
            {
                _identityMap0 = key.GetIdentityMapFactory()();
                return _identityMap0;
            }

            if (_identityMap0.Key == key)
            {
                return _identityMap0;
            }

            if (_identityMap1 == null)
            {
                _identityMap1 = key.GetIdentityMapFactory()();
                return _identityMap1;
            }

            if (_identityMap1.Key == key)
            {
                return _identityMap1;
            }

            if (_identityMaps == null)
            {
                _identityMaps = new Dictionary<IKey, IIdentityMap>();
            }

            IIdentityMap identityMap;
            if (!_identityMaps.TryGetValue(key, out identityMap))
            {
                identityMap = key.GetIdentityMapFactory()();
                _identityMaps[key] = identityMap;
            }
            return identityMap;
        }
        private IIdentityMap FindIdentityMap(IKey key)
        {
            if (_identityMap0 == null)
            {
                return null;
            }

            if (_identityMap0.Key == key)
            {
                return _identityMap0;
            }

            if (_identityMap1 == null)
            {
                return null;
            }

            if (_identityMap1.Key == key)
            {
                return _identityMap1;
            }

            IIdentityMap identityMap;
            if (_identityMaps == null
                || !_identityMaps.TryGetValue(key, out identityMap))
            {
                return null;
            }
            return identityMap;
        }

        public override IEnumerable<InternalEntityEntry> Entries => _entityReferenceMap.Values;

        public override InternalEntityEntry StartTracking(InternalEntityEntry entry)
        {
            var entityType = entry.EntityType;

            if (entry.StateManager != this)
            {
                throw new InvalidOperationException(CoreStrings.WrongStateManager(entityType.Name));
            }

            var mapKey = entry.Entity ?? entry;
            var existingEntry = TryGetEntry(mapKey);

            if (existingEntry == null
                || existingEntry == entry)
            {
                _entityReferenceMap[mapKey] = entry;
            }
            else
            {
                throw new InvalidOperationException(CoreStrings.MultipleEntries(entityType.Name));
            }

            foreach (var key in entityType.GetKeys())
            {
                GetOrCreateIdentityMap(key).Add(entry);
            }

            return entry;
        }

        public override void StopTracking(InternalEntityEntry entry)
        {
            var mapKey = entry.Entity ?? entry;
            _entityReferenceMap.Remove(mapKey);

            foreach (var key in entry.EntityType.GetKeys())
            {
                FindIdentityMap(key)?.Remove(entry);
            }

            if (_referencedUntrackedEntities.HasValue)
            {
                var navigations = entry.EntityType.GetNavigations().ToList();

                foreach (var keyValuePair in _referencedUntrackedEntities.Value.ToList())
                {
                    var entityType = _model.FindEntityType(keyValuePair.Key.GetType());
                    if (navigations.Any(n => n.GetTargetType().IsAssignableFrom(entityType))
                        || entityType.GetNavigations().Any(n => n.GetTargetType().IsAssignableFrom(entry.EntityType)))
                    {
                        _referencedUntrackedEntities.Value.Remove(keyValuePair.Key);

                        var newList = keyValuePair.Value.Where(tuple => tuple.Item2 != entry).ToList();

                        if (newList.Any())
                        {
                            _referencedUntrackedEntities.Value.Add(keyValuePair.Key, newList);
                        }
                    }
                }
            }
        }
        public override void RecordReferencedUntrackedEntity(
            object referencedEntity, INavigation navigation, InternalEntityEntry referencedFromEntry)
        {
            IList<Tuple<INavigation, InternalEntityEntry>> danglers;
            if (!_referencedUntrackedEntities.Value.TryGetValue(referencedEntity, out danglers))
            {
                danglers = new List<Tuple<INavigation, InternalEntityEntry>>();
                _referencedUntrackedEntities.Value.Add(referencedEntity, danglers);
            }
            danglers.Add(Tuple.Create(navigation, referencedFromEntry));
        }

        public override IEnumerable<Tuple<INavigation, InternalEntityEntry>> GetRecordedReferers(object referencedEntity, bool clear)
        {
            IList<Tuple<INavigation, InternalEntityEntry>> danglers;
            if (_referencedUntrackedEntities.HasValue
                && _referencedUntrackedEntities.Value.TryGetValue(referencedEntity, out danglers))
            {
                if (clear)
                {
                    _referencedUntrackedEntities.Value.Remove(referencedEntity);
                }
                return danglers;
            }

            return Enumerable.Empty<Tuple<INavigation, InternalEntityEntry>>();
        }

        public override InternalEntityEntry GetPrincipal(InternalEntityEntry dependentEntry, IForeignKey foreignKey)
        => FindIdentityMap(foreignKey.PrincipalKey)?.TryGetEntry(foreignKey, dependentEntry);

        public override InternalEntityEntry GetPrincipalUsingRelationshipSnapshot(InternalEntityEntry dependentEntry, IForeignKey foreignKey)
            => FindIdentityMap(foreignKey.PrincipalKey)?.TryGetEntryUsingRelationshipSnapshot(foreignKey, dependentEntry);

        public override void UpdateIdentityMap(InternalEntityEntry entry, IKey key)
        {
            if (entry.EntityState == EntityState.Detached)
            {
                return;
            }

            var identityMap = FindIdentityMap(key);
            if (identityMap == null)
            {
                return;
            }

            identityMap.RemoveUsingRelationshipSnapshot(entry);
            identityMap.Add(entry);
        }

        public override void UpdateDependentMap(InternalEntityEntry entry, IForeignKey foreignKey)
        {
            if (entry.EntityState == EntityState.Detached)
            {
                return;
            }

            FindIdentityMap(foreignKey.DeclaringEntityType.FindPrimaryKey())
                ?.FindDependentsMap(foreignKey)
                ?.Update(entry);
        }

        public override IEnumerable<InternalEntityEntry> GetDependents(
            InternalEntityEntry principalEntry, IForeignKey foreignKey)
        {
            var dependentIdentityMap = FindIdentityMap(foreignKey.DeclaringEntityType.FindPrimaryKey());
            return dependentIdentityMap != null
                ? dependentIdentityMap.GetDependentsMap(foreignKey).GetDependents(principalEntry)
                : Enumerable.Empty<InternalEntityEntry>();
        }

        public override IEnumerable<InternalEntityEntry> GetDependentsUsingRelationshipSnapshot(
            InternalEntityEntry principalEntry, IForeignKey foreignKey)
        {
            var dependentIdentityMap = FindIdentityMap(foreignKey.DeclaringEntityType.FindPrimaryKey());
            return dependentIdentityMap != null
                ? dependentIdentityMap.GetDependentsMap(foreignKey).GetDependentsUsingRelationshipSnapshot(principalEntry)
                : Enumerable.Empty<InternalEntityEntry>();
        }
    }
}
