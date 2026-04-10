using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Services;

// ── CategoryService ───────────────────────────────────────

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<IEnumerable<CategoryDto>> GetTreeAsync()
    {
        var all = await _repo.GetTreeAsync();
        // Chỉ trả root (ParentId == null), children đã được nest
        return all.Where(c => c.ParentId == null).Select(MapWithChildren);
    }

    public async Task<IEnumerable<CategoryDto>> GetByParentAsync(Guid? parentId)
    {
        var items = await _repo.GetByParentAsync(parentId);
        return items.Select(c => MapWithChildren(c));
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id)
    {
        var c = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Danh mục Id={id} không tồn tại.");
        return MapWithChildren(c);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest req)
    {
        if (await _repo.SlugExistsAsync(req.Slug))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        if (req.ParentId.HasValue)
            _ = await _repo.GetByIdAsync(req.ParentId.Value)
                ?? throw new KeyNotFoundException("Danh mục cha không tồn tại.");

        var category = new Category
        {
            Name         = req.Name.Trim(),
            Slug         = req.Slug.Trim().ToLower(),
            ParentId     = req.ParentId,
            Description  = req.Description,
            ImageUrl     = req.ImageUrl,
            DisplayOrder = req.DisplayOrder,
            IsActive     = req.IsActive
        };

        await _repo.AddAsync(category);
        await _repo.SaveChangesAsync();
        return await GetByIdAsync(category.Id);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest req)
    {
        var category = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Danh mục Id={id} không tồn tại.");

        if (await _repo.SlugExistsAsync(req.Slug, id))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        // Không cho phép đặt chính nó là parent
        if (req.ParentId == id)
            throw new InvalidOperationException("Danh mục không thể là cha của chính nó.");

        category.Name         = req.Name.Trim();
        category.Slug         = req.Slug.Trim().ToLower();
        category.ParentId     = req.ParentId;
        category.Description  = req.Description;
        category.ImageUrl     = req.ImageUrl;
        category.DisplayOrder = req.DisplayOrder;
        category.IsActive     = req.IsActive;
        category.UpdatedAt    = DateTime.UtcNow;

        _repo.Update(category);
        await _repo.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Danh mục Id={id} không tồn tại.");

        if (await _repo.HasChildrenAsync(id))
            throw new InvalidOperationException("Không thể xóa danh mục đang có danh mục con.");

        if (await _repo.HasProductsAsync(id))
            throw new InvalidOperationException("Không thể xóa danh mục đang có sản phẩm.");

        _repo.Delete(category);
        await _repo.SaveChangesAsync();
    }

    private static CategoryDto MapWithChildren(Category c) =>
        new(c.Id, c.ParentId, c.Name, c.Slug, c.Description, c.ImageUrl,
            c.DisplayOrder, c.IsActive,
            c.Children.Select(MapWithChildren));
}

// ── AuthorService ─────────────────────────────────────────

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _repo;
    public AuthorService(IAuthorRepository repo) => _repo = repo;

    public async Task<IEnumerable<AuthorDto>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(Map);
    }

    public async Task<IEnumerable<AuthorDto>> SearchAsync(string keyword)
    {
        var items = await _repo.SearchAsync(keyword);
        return items.Select(Map);
    }

    public async Task<AuthorDto> GetByIdAsync(Guid id)
    {
        var a = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tác giả Id={id} không tồn tại.");
        return Map(a);
    }

    public async Task<AuthorDto> CreateAsync(CreateAuthorRequest req)
    {
        var author = new Author
        {
            Name        = req.Name.Trim(),
            Bio         = req.Bio,
            AvatarUrl   = req.AvatarUrl,
            Nationality = req.Nationality
        };
        await _repo.AddAsync(author);
        await _repo.SaveChangesAsync();
        return Map(author);
    }

    public async Task<AuthorDto> UpdateAsync(Guid id, CreateAuthorRequest req)
    {
        var author = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tác giả Id={id} không tồn tại.");
        author.Name        = req.Name.Trim();
        author.Bio         = req.Bio;
        author.AvatarUrl   = req.AvatarUrl;
        author.Nationality = req.Nationality;
        author.UpdatedAt   = DateTime.UtcNow;
        _repo.Update(author);
        await _repo.SaveChangesAsync();
        return Map(author);
    }

    public async Task DeleteAsync(Guid id)
    {
        var author = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Tác giả Id={id} không tồn tại.");
        _repo.Delete(author);
        await _repo.SaveChangesAsync();
    }

    private static AuthorDto Map(Author a) =>
        new(a.Id, a.Name, a.Bio, a.AvatarUrl, a.Nationality);
}

// ── PublisherService ──────────────────────────────────────

public class PublisherService : IPublisherService
{
    private readonly IPublisherRepository _repo;
    public PublisherService(IPublisherRepository repo) => _repo = repo;

    public async Task<IEnumerable<PublisherDto>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(Map);
    }

    public async Task<IEnumerable<PublisherDto>> SearchAsync(string keyword)
    {
        var items = await _repo.SearchAsync(keyword);
        return items.Select(Map);
    }

    public async Task<PublisherDto> GetByIdAsync(Guid id)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà xuất bản Id={id} không tồn tại.");
        return Map(p);
    }

    public async Task<PublisherDto> CreateAsync(CreatePublisherRequest req)
    {
        var publisher = new Publisher
        {
            Name    = req.Name.Trim(),
            Country = req.Country,
            Website = req.Website,
            Email   = req.Email
        };
        await _repo.AddAsync(publisher);
        await _repo.SaveChangesAsync();
        return Map(publisher);
    }

    public async Task<PublisherDto> UpdateAsync(Guid id, CreatePublisherRequest req)
    {
        var publisher = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà xuất bản Id={id} không tồn tại.");
        publisher.Name      = req.Name.Trim();
        publisher.Country   = req.Country;
        publisher.Website   = req.Website;
        publisher.Email     = req.Email;
        publisher.UpdatedAt = DateTime.UtcNow;
        _repo.Update(publisher);
        await _repo.SaveChangesAsync();
        return Map(publisher);
    }

    public async Task DeleteAsync(Guid id)
    {
        var publisher = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà xuất bản Id={id} không tồn tại.");
        _repo.Delete(publisher);
        await _repo.SaveChangesAsync();
    }

    private static PublisherDto Map(Publisher p) =>
        new(p.Id, p.Name, p.Country, p.Website, p.Email);
}
