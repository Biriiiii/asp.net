using BookStore.Application.DTOs.Product;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

// ── AuthorsController ─────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _service;
    public AuthorsController(IAuthorService service) => _service = service;

    /// <summary>Lấy tất cả tác giả</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuthorDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    /// <summary>Tìm kiếm tác giả theo tên</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<AuthorDto>), 200)]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _service.SearchAsync(keyword);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết tác giả theo Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuthorDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tạo tác giả mới [Admin/ContentManager]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(AuthorDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateAuthorRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật tác giả [Admin/ContentManager]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(AuthorDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAuthorRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Xóa tác giả [Admin]</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

// ── PublishersController ──────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PublishersController : ControllerBase
{
    private readonly IPublisherService _service;
    public PublishersController(IPublisherService service) => _service = service;

    /// <summary>Lấy tất cả nhà xuất bản</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PublisherDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    /// <summary>Tìm kiếm nhà xuất bản theo tên</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<PublisherDto>), 200)]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _service.SearchAsync(keyword);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết nhà xuất bản theo Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PublisherDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tạo nhà xuất bản mới [Admin/ContentManager]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(PublisherDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreatePublisherRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật nhà xuất bản [Admin/ContentManager]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(typeof(PublisherDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePublisherRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Xóa nhà xuất bản [Admin]</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
