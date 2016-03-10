using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore.Infrastructure.Internal
{
    public class MaterializingSqlServerModelSource : SqlServerModelSource
    {
        public MaterializingSqlServerModelSource(IDbSetFinder setFinder, ICoreConventionSetBuilder coreConventionSetBuilder, IModelCustomizer modelCustomizer, IModelCacheKeyFactory modelCacheKeyFactory)
            : base(setFinder, coreConventionSetBuilder, modelCustomizer, modelCacheKeyFactory)
        {
        }

        protected override IModel CreateModel(
            DbContext context,
            IConventionSetBuilder conventionSetBuilder,
            IModelValidator validator)
        {

            var conventionSet = CreateConventionSet(conventionSetBuilder);

            var modelBuilder = new MaterializingModelBuilder(conventionSet);
            var internalModelBuilder = ((IInfrastructure<InternalModelBuilder>)modelBuilder).Instance;

            internalModelBuilder.Metadata.SetProductVersion(ProductInfo.GetVersion());

            FindSets(modelBuilder, context);

            ModelCustomizer.Customize(modelBuilder, context);

            internalModelBuilder.Validate();

            validator.Validate(modelBuilder.Model);

            return modelBuilder.Model;
        }
    }
}