using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

// ── Generic base ──────────────────────────────────────────

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db  = db;
        _set = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id) =>
        await _set.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() =>
        await _set.AsNoTracking().ToListAsync();

    public virtual async Task AddAsync(T entity) =>
        await _set.AddAsync(entity);

    public virtual void Update(T entity) =>
        _set.Update(entity);

    public virtual void Delete(T entity) =>
        _set.Remove(entity);

    public async Task<int> SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}

// ── ProductRepository ─────────────────────────────────────

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<Product?> GetBySlugAsync(string slug) =>
        await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Publisher)
            .Include(p => p.ProductAuthors).ThenInclude(pa => pa.Author)
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<Product?> GetDetailAsync(Guid id) =>
        await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Publisher)
            .Include(p => p.ProductAuthors).ThenInclude(pa => pa.Author)
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(ProductFilter f)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Publisher)
            .Include(p => p.ProductAuthors).ThenInclude(pa => pa.Author)
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .AsQueryable();

        // ── Filters ──
        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var kw = f.Keyword.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(kw) ||
                (p.Isbn != null && p.Isbn.Contains(kw)) ||
                p.ProductAuthors.Any(pa => pa.Author.Name.ToLower().Contains(kw)));
        }

        if (f.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == f.CategoryId.Value);

        if (f.AuthorId.HasValue)
            query = query.Where(p => p.ProductAuthors.Any(pa => pa.AuthorId == f.AuthorId.Value));

        if (f.PublisherId.HasValue)
            query = query.Where(p => p.PublisherId == f.PublisherId.Value);

        if (f.MinPrice.HasValue)
            query = query.Where(p => p.SalePrice >= f.MinPrice.Value);

        if (f.MaxPrice.HasValue)
            query = query.Where(p => p.SalePrice <= f.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(f.Language))
            query = query.Where(p => p.Language == f.Language);

        if (f.IsActive.HasValue)
            query = query.IgnoreQueryFilters().Where(p => p.IsActive == f.IsActive.Value);

        if (f.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == f.IsFeatured.Value);

        if (f.InStockOnly == true)
            query = query.Where(p => p.Inventory != null && p.Inventory.QtyAvailable > p.Inventory.QtyReserved);

        // ── Sort ──
        query = f.SortBy switch
        {
            "price_asc"   => query.OrderBy(p => p.SalePrice),
            "price_desc"  => query.OrderByDescending(p => p.SalePrice),
            "bestseller"  => query.OrderByDescending(p => p.Inventory != null ? p.Inventory.QtySold : 0),
            _             => query.OrderByDescending(p => p.CreatedAt)  // newest (default)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null) =>
        await _db.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId));
}

// ── CategoryRepository ────────────────────────────────────

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Category>> GetTreeAsync() =>
        await _db.Categories.IgnoreQueryFilters()
            .Include(c => c.Children)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Category>> GetByParentAsync(Guid? parentId) =>
        await _db.Categories.IgnoreQueryFilters()
            .Include(c => c.Children)
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Category?> GetBySlugAsync(string slug) =>
        await _db.Categories.IgnoreQueryFilters()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null) =>
        await _db.Categories.IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId));

    public async Task<bool> HasChildrenAsync(Guid id) =>
        await _db.Categories.IgnoreQueryFilters().AnyAsync(c => c.ParentId == id);

    public async Task<bool> HasProductsAsync(Guid id) =>
        await _db.Products.IgnoreQueryFilters().AnyAsync(p => p.CategoryId == id);
}

// ── AuthorRepository ──────────────────────────────────────

public class AuthorRepository : Repository<Author>, IAuthorRepository
{
    public AuthorRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Author>> SearchAsync(string keyword) =>
        await _db.Authors
            .Where(a => a.Name.ToLower().Contains(keyword.ToLower()))
            .OrderBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync();
}

// ── PublisherRepository ───────────────────────────────────

public class PublisherRepository : Repository<Publisher>, IPublisherRepository
{
    public PublisherRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Publisher>> SearchAsync(string keyword) =>
        await _db.Publishers
            .Where(p => p.Name.ToLower().Contains(keyword.ToLower()))
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
}

// ── InventoryRepository ───────────────────────────────────

public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(AppDbContext db) : base(db) { }

    public async Task<Inventory?> GetByProductIdAsync(Guid productId) =>
        await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);

    public async Task<IEnumerable<Inventory>> GetLowStockAsync() =>
        await _db.Inventories
            .Include(i => i.Product)
            .Where(i => (i.QtyAvailable - i.QtyReserved) <= i.MinThreshold)
            .AsNoTracking()
            .ToListAsync();
}
