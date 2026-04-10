using BookStore.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Product;

// ── Responses ────────────────────────────────────────────

public record ProductListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string? Isbn,
    decimal OriginalPrice,
    decimal SalePrice,
    int DiscountPercent,
    string? PrimaryImageUrl,
    string CategoryName,
    IEnumerable<string> AuthorNames,
    bool InStock,
    bool IsFeatured,
    DateTime CreatedAt
);

public record ProductDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Isbn,
    int PageCount,
    int WeightGram,
    string Language,
    string CoverType,
    decimal OriginalPrice,
    decimal SalePrice,
    int DiscountPercent,
    string? Description,
    bool IsActive,
    bool IsFeatured,
    DateTime? PublishedDate,
    CategorySummaryDto Category,
    PublisherSummaryDto Publisher,
    IEnumerable<ProductAuthorDto> Authors,
    IEnumerable<ProductImageDto> Images,
    InventoryDto? Inventory,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CategorySummaryDto(Guid Id, string Name, string Slug);
public record PublisherSummaryDto(Guid Id, string Name, string? Country);
public record ProductAuthorDto(Guid AuthorId, string Name, string Role, string? AvatarUrl);
public record ProductImageDto(Guid Id, string ImageUrl, string? AltText, bool IsPrimary, int DisplayOrder);
public record InventoryDto(int QtyAvailable, int QtyReserved, int QtyActual, int MinThreshold, bool IsLowStock, bool IsOutOfStock, string? WarehouseLocation);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

// ── Category DTOs ─────────────────────────────────────────

public record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int DisplayOrder,
    bool IsActive,
    IEnumerable<CategoryDto> Children
);

// ── Author DTOs ───────────────────────────────────────────

public record AuthorDto(Guid Id, string Name, string? Bio, string? AvatarUrl, string? Nationality);

// ── Publisher DTOs ────────────────────────────────────────

public record PublisherDto(Guid Id, string Name, string? Country, string? Website, string? Email);

// ── Requests ──────────────────────────────────────────────

public class CreateProductRequest
{
    [Required] [MaxLength(500)] public string Title { get; set; } = string.Empty;
    [Required] [MaxLength(500)] public string Slug { get; set; } = string.Empty;
    [MaxLength(20)] public string? Isbn { get; set; }
    [Required] public Guid CategoryId { get; set; }
    [Required] public Guid PublisherId { get; set; }
    [Range(1, 99999)] public int PageCount { get; set; }
    [Range(1, 99999)] public int WeightGram { get; set; }
    [MaxLength(10)] public string Language { get; set; } = "vi";
    public CoverType CoverType { get; set; } = CoverType.Paperback;
    [Range(0, 999999999)] public decimal OriginalPrice { get; set; }
    [Range(0, 999999999)] public decimal SalePrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public DateTime? PublishedDate { get; set; }
    public List<ProductAuthorRequest> Authors { get; set; } = new();
    public List<ProductImageRequest> Images { get; set; } = new();
}

public class UpdateProductRequest : CreateProductRequest { }

public record ProductAuthorRequest(Guid AuthorId, string Role = "Author");
public record ProductImageRequest(string ImageUrl, string? AltText, bool IsPrimary, int DisplayOrder);

public class CreateCategoryRequest
{
    [Required] [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] [MaxLength(200)] public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest : CreateCategoryRequest { }

public class CreateAuthorRequest
{
    [Required] [MaxLength(200)] public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Nationality { get; set; }
}

public class CreatePublisherRequest
{
    [Required] [MaxLength(200)] public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
}

public class UpdateInventoryRequest
{
    [Range(0, 999999)] public int QtyAvailable { get; set; }
    [Range(0, 9999)] public int MinThreshold { get; set; } = 5;
    [MaxLength(100)] public string? WarehouseLocation { get; set; }
}

public class ProductQueryParams
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? PublisherId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Language { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? InStockOnly { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
