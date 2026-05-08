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

    /// <summary>Export danh sách sản phẩm ra file Excel</summary>
    [HttpGet("export-excel")]
    [Authorize(Roles = "Admin,ContentManager,Staff")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportExcel([FromQuery] ProductQueryParams query)
    {
        var bytes = await _service.ExportExcelAsync(query);
        var fileName = $"products-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Import sản phẩm từ file Excel (.xlsx). Có Id thì cập nhật, không có Id thì tạo mới theo Slug.</summary>
    [HttpPost("import-excel")]
    [HttpPut("import-excel")]
    [Authorize(Roles = "Admin,ContentManager")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductExcelImportResultDto), 200)]
    public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn file Excel." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _service.ImportExcelAsync(stream);
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
