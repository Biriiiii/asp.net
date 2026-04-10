namespace BookStore.Domain.Entities;

public class ProductAuthor
{
    public Guid ProductId { get; set; }
    public Guid AuthorId { get; set; }
    public string Role { get; set; } = "Author"; // Author, Translator, Editor...

    // Navigation
    public Product Product { get; set; } = null!;
    public Author Author { get; set; } = null!;
}
