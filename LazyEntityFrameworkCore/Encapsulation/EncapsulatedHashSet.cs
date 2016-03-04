using System;
using System.Collections.Generic;
using LazyEntityFrameworkCore.Encapsulation.Builders;

namespace LazyEntityFrameworkCore.Encapsulation
{
    public class EncapsulatedHashSet<T> : HashSet<T>, IEncapsulatedCollection<T>
    {
        public IBuilder<T> GetBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
