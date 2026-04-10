using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using BookStore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;
    public AdminUsersController(IAdminUserService service) => _service = service;

    /// <summary>Lấy danh sách người dùng có phân trang + lọc</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] UserQueryParams query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết người dùng theo Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tạo tài khoản nhân viên/admin [SuperAdmin]</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(UserProfileDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] AdminCreateUserRequest request)
    {
        var result = await _service.CreateUserAsync(request);
        return StatusCode(201, result);
    }

    /// <summary>Kích hoạt tài khoản</summary>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _service.ActivateAsync(id);
        return Ok(new { message = "Tài khoản đã được kích hoạt." });
    }

    /// <summary>Vô hiệu hóa tài khoản</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok(new { message = "Tài khoản đã bị vô hiệu hóa." });
    }

    /// <summary>Khóa tài khoản tạm thời</summary>
    [HttpPatch("{id:guid}/lock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Lock(Guid id, [FromQuery] int minutes = 30)
    {
        await _service.LockUserAsync(id, minutes);
        return Ok(new { message = $"Tài khoản đã bị khóa {minutes} phút." });
    }

    /// <summary>Mở khóa tài khoản</summary>
    [HttpPatch("{id:guid}/unlock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Unlock(Guid id)
    {
        await _service.UnlockUserAsync(id);
        return Ok(new { message = "Tài khoản đã được mở khóa." });
    }

    /// <summary>Gán role cho người dùng [SuperAdmin]</summary>
    [HttpPost("{id:guid}/roles")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        await _service.AssignRoleAsync(id, request);
        return Ok(new { message = $"Đã gán role {request.Role}." });
    }

    /// <summary>Xóa role của người dùng [SuperAdmin]</summary>
    [HttpDelete("{id:guid}/roles/{role}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRole(Guid id, UserRole role)
    {
        await _service.RemoveRoleAsync(id, role);
        return Ok(new { message = $"Đã xóa role {role}." });
    }
}
