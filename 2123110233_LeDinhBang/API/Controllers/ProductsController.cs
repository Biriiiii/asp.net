using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    /// <summary>Lấy danh sách sản phẩm có phân trang + lọc</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết sản phẩm theo Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return Ok(product);
    }

    /// <summary>Lấy chi tiết sản phẩm theo Slug (SEO)</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _service.GetBySlugAsync(slug);
        return Ok(product);
    }

    /// <summary>Tạo sản phẩm mới [Admin/ContentManager]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(ProductDetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Cập nhật sản phẩm [Admin/ContentManager]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(ProductDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _service.UpdateAsync(id, request);
        return Ok(product);
    }

    /// <summary>Xóa mềm sản phẩm (IsActive = false) [Admin]</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Cập nhật tồn kho sản phẩm [Admin/Staff]</summary>
    [HttpPatch("{id:guid}/inventory")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(InventoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateInventory(Guid id, [FromBody] UpdateInventoryRequest request)
    {
        var inv = await _service.UpdateInventoryAsync(id, request);
        return Ok(inv);
    }
}
