using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace LazyEntityFrameworkCore.ChangeTracking.Internal
{
    public class LazyStateManager : StateManager
    {
        public LazyStateManager(IInternalEntityEntryFactory factory, IInternalEntityEntrySubscriber subscriber, 
            IInternalEntityEntryNotifier notifier, IValueGenerationManager valueGeneration, IModel model, 
            IDatabase database, IConcurrencyDetector concurrencyDetector, DbContext context) 
            : base(factory, subscriber, notifier, valueGeneration, model, database, concurrencyDetector, context)
        {
        }

        public bool InTracking { get; set; } = false;

        public override InternalEntityEntry StartTrackingFromQuery(IEntityType entityType, object entity, ValueBuffer valueBuffer)
        {
            InTracking = true;
            try
            {
                return base.StartTrackingFromQuery(entityType, entity, valueBuffer);
            }
            finally
            {
                InTracking = false;
            }
        }

     }
}
