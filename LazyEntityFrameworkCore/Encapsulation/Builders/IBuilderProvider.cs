using Microsoft.EntityFrameworkCore;

namespace LazyEntityFrameworkCore.Encapsulation.Builders
{
    public interface IBuilderProvider
    {
        IBuilderProvider Register<T, TBuilder>() where T: class where TBuilder : IBuilder<T> ;
        IBuilder<T> GetBuilder<T>(DbContext context) where T : class;
    }
}
