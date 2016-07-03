using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.ChangeTracking.Internal
{
    public class LazyStateManager : StateManager
    {
        public LazyStateManager(
            IInternalEntityEntryFactory factory,
            IInternalEntityEntrySubscriber subscriber, 
            IInternalEntityEntryNotifier notifier,
            IValueGenerationManager valueGeneration,
            IModel model, 
            IDatabase database,
            IConcurrencyDetector concurrencyDetector,
            ICurrentDbContext currentContext) 
            : base(factory, subscriber, notifier, valueGeneration, model, database, concurrencyDetector, currentContext)
        {
        }

        public bool InTracking { get; set; }
        public override InternalEntityEntry StartTrackingFromQuery(IEntityType baseEntityType, object entity, ValueBuffer valueBuffer, ISet<IForeignKey> handledForeignKeys)
        {
            InTracking = true;
            try
            {
                return base.StartTrackingFromQuery(baseEntityType, entity, valueBuffer, handledForeignKeys);
            }
            finally
            {
                InTracking = false;
            }
        }
    }
}
