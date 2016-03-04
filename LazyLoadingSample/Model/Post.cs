namespace LazyLoadingSample.Model
{
    public class Post
    {
        private int _PostId;
        private string _Title;
        private string _Content;
        private int _BlogId;
        private Blog _Blog;

        public int PostId
        {
            get { return _PostId; }
        }

        public string Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        public string Content
        {
            get { return _Content; }
            set { _Content = value; }
        }

        public int BlogId
        {
            get { return _BlogId; }
        }

        public virtual Blog Blog
        {
            get { return _Blog; }
        }

        protected Post() { }

        public Post(Blog blog, string content)
        {
            _Blog = blog;
            _Content = content;
        }
    }
}