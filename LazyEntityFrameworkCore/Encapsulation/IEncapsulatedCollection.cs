using System.Collections.Generic;
using LazyEntityFrameworkCore.Encapsulation.Builders;

namespace LazyEntityFrameworkCore.Encapsulation
{
    public interface IEncapsulatedCollection<T> : ICollection<T>
    {
        IBuilder<T> GetBuilder();
    }
}
