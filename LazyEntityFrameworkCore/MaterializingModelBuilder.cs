using LazyEntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LazyEntityFrameworkCore
{
    public class MaterializingModelBuilder : ModelBuilder, IInfrastructure<InternalModelBuilder>
    {
        private readonly InternalModelBuilder _builder;
        public MaterializingModelBuilder(ConventionSet conventions) : base(conventions)
        {
            _builder = new InternalModelBuilder(new MaterializingModel(conventions));
        }

        InternalModelBuilder IInfrastructure<InternalModelBuilder>.Instance => _builder;
    }
}