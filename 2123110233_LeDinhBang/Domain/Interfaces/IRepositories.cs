using BookStore.Domain.Entities;

namespace BookStore.Domain.Interfaces;

// ── Generic ──────────────────────────────────────────────
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
}

// ── Product ───────────────────────────────────────────────
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<Product?> GetDetailAsync(Guid id); // include authors, images, inventory
    Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(ProductFilter filter);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}

// ── Category ──────────────────────────────────────────────
public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetTreeAsync();           // full tree
    Task<IEnumerable<Category>> GetByParentAsync(Guid? parentId);
    Task<Category?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
    Task<bool> HasChildrenAsync(Guid id);
    Task<bool> HasProductsAsync(Guid id);
}

// ── Author ────────────────────────────────────────────────
public interface IAuthorRepository : IRepository<Author>
{
    Task<IEnumerable<Author>> SearchAsync(string keyword);
}

// ── Publisher ─────────────────────────────────────────────
public interface IPublisherRepository : IRepository<Publisher>
{
    Task<IEnumerable<Publisher>> SearchAsync(string keyword);
}

// ── Inventory ─────────────────────────────────────────────
public interface IInventoryRepository : IRepository<Inventory>
{
    Task<Inventory?> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<Inventory>> GetLowStockAsync();
}

// ── Filter model ──────────────────────────────────────────
public class ProductFilter
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
    public string SortBy { get; set; } = "newest";   // newest | bestseller | price_asc | price_desc | rating
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
