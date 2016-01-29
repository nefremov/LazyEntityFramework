using System.Collections.Generic;

namespace LazyLoadingSample.Model
{
    public class Blog
    {
        public virtual int BlogId { get; set; }
        public virtual string Url { get; set; }

        public virtual ICollection<Post> Posts
        {
            get;
            set;
        }
    }
}