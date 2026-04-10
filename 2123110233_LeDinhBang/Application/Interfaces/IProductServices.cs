using BookStore.Application.DTOs.Product;

namespace BookStore.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams query);
    Task<ProductDetailDto> GetByIdAsync(Guid id);
    Task<ProductDetailDto> GetBySlugAsync(string slug);
    Task<ProductDetailDto> CreateAsync(CreateProductRequest request);
    Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
    Task<InventoryDto> UpdateInventoryAsync(Guid productId, UpdateInventoryRequest request);
}

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetTreeAsync();
    Task<IEnumerable<CategoryDto>> GetByParentAsync(Guid? parentId);
    Task<CategoryDto> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task DeleteAsync(Guid id);
}

public interface IAuthorService
{
    Task<IEnumerable<AuthorDto>> GetAllAsync();
    Task<IEnumerable<AuthorDto>> SearchAsync(string keyword);
    Task<AuthorDto> GetByIdAsync(Guid id);
    Task<AuthorDto> CreateAsync(CreateAuthorRequest request);
    Task<AuthorDto> UpdateAsync(Guid id, CreateAuthorRequest request);
    Task DeleteAsync(Guid id);
}

public interface IPublisherService
{
    Task<IEnumerable<PublisherDto>> GetAllAsync();
    Task<IEnumerable<PublisherDto>> SearchAsync(string keyword);
    Task<PublisherDto> GetByIdAsync(Guid id);
    Task<PublisherDto> CreateAsync(CreatePublisherRequest request);
    Task<PublisherDto> UpdateAsync(Guid id, CreatePublisherRequest request);
    Task DeleteAsync(Guid id);
}
