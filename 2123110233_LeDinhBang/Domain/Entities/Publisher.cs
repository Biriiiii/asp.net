namespace BookStore.Domain.Entities;

public class Publisher : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
