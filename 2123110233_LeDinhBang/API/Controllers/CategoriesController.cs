using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;
    public CategoriesController(ICategoryService service) => _service = service;

    /// <summary>Lấy cây danh mục đầy đủ</summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
    public async Task<IActionResult> GetTree()
    {
        var result = await _service.GetTreeAsync();
        return Ok(result);
    }

    /// <summary>Lấy danh mục con theo parentId (null = root)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
    public async Task<IActionResult> GetByParent([FromQuery] Guid? parentId)
    {
        var result = await _service.GetByParentAsync(parentId);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết danh mục theo Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tạo danh mục mới [Admin/ContentManager]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật danh mục [Admin/ContentManager]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Xóa danh mục [Admin]</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
