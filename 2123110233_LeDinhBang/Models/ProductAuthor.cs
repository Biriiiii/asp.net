namespace _2123110233_LeDinhBang.Models
{
    public class ProductAuthor
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public Guid AuthorId { get; set; }
        public Author Author { get; set; }
    }
}
