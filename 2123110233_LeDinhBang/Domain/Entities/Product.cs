using BookStore.Domain.Enums;

namespace BookStore.Domain.Entities;

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Guid PublisherId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public int PageCount { get; set; }
    public int WeightGram { get; set; }
    public string Language { get; set; } = "vi";
    public CoverType CoverType { get; set; } = CoverType.Paperback;

    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public DateTime? PublishedDate { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Publisher Publisher { get; set; } = null!;
    public ICollection<ProductAuthor> ProductAuthors { get; set; } = new List<ProductAuthor>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public Inventory? Inventory { get; set; }
}
