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

        private readonly Dictionary<object, WeakReference<InternalEntityEntry>> _detachedEntityReferenceMap
            = new Dictionary<object, WeakReference<InternalEntityEntry>>(ReferenceEqualityComparer.Instance);

        private readonly LazyRef<IDictionary<IForeignKey, IList<InternalEntityEntry>>> _danglingDependents
            = new LazyRef<IDictionary<IForeignKey, IList<InternalEntityEntry>>>(
                () => new Dictionary<IForeignKey, IList<InternalEntityEntry>>());

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
            DbContext context) 
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
                if (_detachedEntityReferenceMap.Count % 100 == 99)
                {
                    InternalEntityEntry _;
                    var deadKeys = _detachedEntityReferenceMap
                        .Where(e => !e.Value.TryGetTarget(out _))
                        .Select(e => e.Key)
                        .ToList();

                    foreach (var deadKey in deadKeys)
                    {
                        _detachedEntityReferenceMap.Remove(deadKey);
                    }
                }

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

                entry = _subscriber.SnapshotAndSubscribe(_factory.Create(this, entityType, entity));

                _detachedEntityReferenceMap[entity] = new WeakReference<InternalEntityEntry>(entry);
            }
            return entry;
        }
        public bool InTracking { get; set; }
        public override InternalEntityEntry StartTrackingFromQuery(IEntityType entityType, object entity, ValueBuffer valueBuffer)
        {
            InTracking = true;
            try
            {
                var existingEntry = TryGetEntry(entity);
                if (existingEntry != null)
                {
                    return existingEntry;
                }

                var newEntry = _factory.Create(this, entityType, entity, valueBuffer);

                _subscriber.SnapshotAndSubscribe(newEntry);

                foreach (var key in entityType.GetKeys())
                {
                    GetOrCreateIdentityMap(key).AddOrUpdate(newEntry);
                }

                _entityReferenceMap[entity] = newEntry;
                _detachedEntityReferenceMap.Remove(entity);

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
            if (!_entityReferenceMap.TryGetValue(entity, out entry))
            {
                WeakReference<InternalEntityEntry> detachedEntry;

                if (!_detachedEntityReferenceMap.TryGetValue(entity, out detachedEntry)
                    || !detachedEntry.TryGetTarget(out entry))
                {
                    return null;
                }
            }

            return entry;
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
                _detachedEntityReferenceMap.Remove(mapKey);
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
            _detachedEntityReferenceMap[mapKey] = new WeakReference<InternalEntityEntry>(entry);

            foreach (var key in entry.EntityType.GetKeys())
            {
                FindIdentityMap(key)?.Remove(entry);
            }

            if (_danglingDependents.HasValue)
            {
                foreach (var foreignKey in entry.EntityType.GetForeignKeys())
                {
                    IList<InternalEntityEntry> entries;
                    if (_danglingDependents.Value.TryGetValue(foreignKey, out entries)
                        && entries.Remove(entry)
                        && entries.Count == 0)
                    {
                        _danglingDependents.Value.Remove(foreignKey);
                    }
                }
            }
        }
        public override void RecordDanglingDependent(IForeignKey foreignKey, InternalEntityEntry entry)
        {
            IList<InternalEntityEntry> entries;
            if (!_danglingDependents.Value.TryGetValue(foreignKey, out entries))
            {
                entries = new List<InternalEntityEntry>();
                _danglingDependents.Value[foreignKey] = entries;
            }
            entries.Add(entry);
        }

        public override IEnumerable<InternalEntityEntry> GetDanglingDependents(IForeignKey foreignKey, InternalEntityEntry entry)
        {
            IList<InternalEntityEntry> entries;
            if (_danglingDependents.HasValue
                && _danglingDependents.Value.TryGetValue(foreignKey, out entries))
            {
                var matchingDependents = entries
                    .Where(e => e[foreignKey.DependentToPrincipal] == entry.Entity)
                    .ToList();

                if (matchingDependents.Count > 0)
                {
                    foreach (var dependentEntry in matchingDependents)
                    {
                        entries.Remove(dependentEntry);
                    }

                    if (entries.Count == 0)
                    {
                        _danglingDependents.Value.Remove(foreignKey);
                    }
                }

                return matchingDependents;
            }

            return Enumerable.Empty<InternalEntityEntry>();
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
