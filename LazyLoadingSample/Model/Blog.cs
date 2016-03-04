using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;
using LazyEntityFrameworkCore.Encapsulation;

namespace LazyLoadingSample.Model
{
    public class Blog
    {
        private int _blogId;
        private string _url;
        private IEncapsulatedCollection<Post> _posts = new EncapsulatedHashSet<Post>();

        public int BlogId
        {
            get { return _blogId; }
            //set { _blogId = value; }
        }

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        public virtual IEncapsulatedCollection<Post> Posts
        {
            get { return _posts; }
        }
    }
}