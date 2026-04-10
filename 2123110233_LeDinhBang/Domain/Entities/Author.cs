namespace BookStore.Domain.Entities;

public class Author : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Nationality { get; set; }

    // Navigation
    public ICollection<ProductAuthor> ProductAuthors { get; set; } = new List<ProductAuthor>();
}
