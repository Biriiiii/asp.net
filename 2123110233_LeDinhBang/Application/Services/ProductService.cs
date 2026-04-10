using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IPublisherRepository _publishers;
    private readonly IAuthorRepository _authors;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IPublisherRepository publishers,
        IAuthorRepository authors)
    {
        _products = products;
        _categories = categories;
        _publishers = publishers;
        _authors = authors;
    }

    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams query)
    {
        var filter = new ProductFilter
        {
            Keyword    = query.Keyword,
            CategoryId = query.CategoryId,
            AuthorId   = query.AuthorId,
            PublisherId= query.PublisherId,
            MinPrice   = query.MinPrice,
            MaxPrice   = query.MaxPrice,
            Language   = query.Language,
            IsActive   = query.IsActive,
            IsFeatured = query.IsFeatured,
            InStockOnly= query.InStockOnly,
            SortBy     = query.SortBy,
            Page       = query.Page,
            PageSize   = query.PageSize
        };

        var (items, total) = await _products.GetPagedAsync(filter);
        var dtos = items.Select(MapToListItem);
        return new PagedResult<ProductListItemDto>(dtos, total, query.Page, query.PageSize);
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id)
    {
        var product = await _products.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm với Id: {id}");
        return MapToDetail(product);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug)
    {
        var product = await _products.GetBySlugAsync(slug)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm với slug: {slug}");
        return MapToDetail(product);
    }

    public async Task<ProductDetailDto> CreateAsync(CreateProductRequest req)
    {
        // Validate slug unique
        if (await _products.SlugExistsAsync(req.Slug))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        // Validate foreign keys
        var category  = await _categories.GetByIdAsync(req.CategoryId)
            ?? throw new KeyNotFoundException("Danh mục không tồn tại.");
        var publisher = await _publishers.GetByIdAsync(req.PublisherId)
            ?? throw new KeyNotFoundException("Nhà xuất bản không tồn tại.");

        var product = new Product
        {
            CategoryId    = req.CategoryId,
            PublisherId   = req.PublisherId,
            Title         = req.Title.Trim(),
            Slug          = req.Slug.Trim().ToLower(),
            Isbn          = req.Isbn?.Trim(),
            PageCount     = req.PageCount,
            WeightGram    = req.WeightGram,
            Language      = req.Language,
            CoverType     = req.CoverType,
            OriginalPrice = req.OriginalPrice,
            SalePrice     = req.SalePrice,
            Description   = req.Description,
            IsActive      = req.IsActive,
            IsFeatured    = req.IsFeatured,
            PublishedDate = req.PublishedDate
        };

        // Thêm authors
        foreach (var a in req.Authors)
        {
            var author = await _authors.GetByIdAsync(a.AuthorId)
                ?? throw new KeyNotFoundException($"Tác giả Id={a.AuthorId} không tồn tại.");
            product.ProductAuthors.Add(new ProductAuthor
            {
                AuthorId = a.AuthorId,
                Role     = a.Role
            });
        }

        // Thêm images
        foreach (var img in req.Images)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl     = img.ImageUrl,
                AltText      = img.AltText,
                IsPrimary    = img.IsPrimary,
                DisplayOrder = img.DisplayOrder
            });
        }

        // Tạo inventory mặc định
        product.Inventory = new Inventory { ProductId = product.Id };

        await _products.AddAsync(product);
        await _products.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductRequest req)
    {
        var product = await _products.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {id}");

        // Validate slug unique (bỏ qua chính nó)
        if (await _products.SlugExistsAsync(req.Slug, id))
            throw new InvalidOperationException($"Slug '{req.Slug}' đã tồn tại.");

        _ = await _categories.GetByIdAsync(req.CategoryId)
            ?? throw new KeyNotFoundException("Danh mục không tồn tại.");
        _ = await _publishers.GetByIdAsync(req.PublisherId)
            ?? throw new KeyNotFoundException("Nhà xuất bản không tồn tại.");

        product.CategoryId    = req.CategoryId;
        product.PublisherId   = req.PublisherId;
        product.Title         = req.Title.Trim();
        product.Slug          = req.Slug.Trim().ToLower();
        product.Isbn          = req.Isbn?.Trim();
        product.PageCount     = req.PageCount;
        product.WeightGram    = req.WeightGram;
        product.Language      = req.Language;
        product.CoverType     = req.CoverType;
        product.OriginalPrice = req.OriginalPrice;
        product.SalePrice     = req.SalePrice;
        product.Description   = req.Description;
        product.IsActive      = req.IsActive;
        product.IsFeatured    = req.IsFeatured;
        product.PublishedDate = req.PublishedDate;
        product.UpdatedAt     = DateTime.UtcNow;

        // Cập nhật authors: xóa hết, thêm lại
        product.ProductAuthors.Clear();
        foreach (var a in req.Authors)
        {
            _ = await _authors.GetByIdAsync(a.AuthorId)
                ?? throw new KeyNotFoundException($"Tác giả Id={a.AuthorId} không tồn tại.");
            product.ProductAuthors.Add(new ProductAuthor
            {
                ProductId = product.Id,
                AuthorId  = a.AuthorId,
                Role      = a.Role
            });
        }

        // Cập nhật images: xóa hết, thêm lại
        product.Images.Clear();
        foreach (var img in req.Images)
        {
            product.Images.Add(new ProductImage
            {
                ProductId    = product.Id,
                ImageUrl     = img.ImageUrl,
                AltText      = img.AltText,
                IsPrimary    = img.IsPrimary,
                DisplayOrder = img.DisplayOrder
            });
        }

        _products.Update(product);
        await _products.SaveChangesAsync();
        return await GetByIdAsync(product.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _products.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {id}");
        // Soft delete
        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;
        _products.Update(product);
        await _products.SaveChangesAsync();
    }

    public async Task<InventoryDto> UpdateInventoryAsync(Guid productId, UpdateInventoryRequest req)
    {
        var product = await _products.GetDetailAsync(productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id: {productId}");

        var inv = product.Inventory ?? new Inventory { ProductId = productId };
        inv.QtyAvailable      = req.QtyAvailable;
        inv.MinThreshold      = req.MinThreshold;
        inv.WarehouseLocation = req.WarehouseLocation;
        inv.UpdatedAt         = DateTime.UtcNow;

        if (product.Inventory == null)
        {
            product.Inventory = inv;
            await _products.SaveChangesAsync();
        }
        else
        {
            _products.Update(product);
            await _products.SaveChangesAsync();
        }

        return MapInventory(inv);
    }

    // ── Mappers ──────────────────────────────────────────

    private static ProductListItemDto MapToListItem(Product p)
    {
        var discount = p.OriginalPrice > 0
            ? (int)Math.Round((p.OriginalPrice - p.SalePrice) / p.OriginalPrice * 100)
            : 0;
        return new ProductListItemDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Isbn,
            p.OriginalPrice,
            p.SalePrice,
            discount,
            p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl,
            p.Category?.Name ?? "",
            p.ProductAuthors.Select(pa => pa.Author?.Name ?? ""),
            p.Inventory?.QtyActual > 0,
            p.IsFeatured,
            p.CreatedAt
        );
    }

    private static ProductDetailDto MapToDetail(Product p)
    {
        var discount = p.OriginalPrice > 0
            ? (int)Math.Round((p.OriginalPrice - p.SalePrice) / p.OriginalPrice * 100)
            : 0;
        return new ProductDetailDto(
            p.Id, p.Title, p.Slug, p.Isbn,
            p.PageCount, p.WeightGram, p.Language, p.CoverType.ToString(),
            p.OriginalPrice, p.SalePrice, discount,
            p.Description, p.IsActive, p.IsFeatured, p.PublishedDate,
            new CategorySummaryDto(p.Category.Id, p.Category.Name, p.Category.Slug),
            new PublisherSummaryDto(p.Publisher.Id, p.Publisher.Name, p.Publisher.Country),
            p.ProductAuthors.Select(pa => new ProductAuthorDto(pa.AuthorId, pa.Author?.Name ?? "", pa.Role, pa.Author?.AvatarUrl)),
            p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.DisplayOrder)),
            p.Inventory == null ? null : MapInventory(p.Inventory),
            p.CreatedAt, p.UpdatedAt
        );
    }

    private static InventoryDto MapInventory(Inventory inv) =>
        new(inv.QtyAvailable, inv.QtyReserved, inv.QtyActual, inv.MinThreshold, inv.IsLowStock, inv.IsOutOfStock, inv.WarehouseLocation);
}
