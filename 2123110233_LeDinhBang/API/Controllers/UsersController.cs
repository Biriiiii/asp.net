using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    public UsersController(IUserService service) => _service = service;

    // ── Profile ───────────────────────────────────────────

    /// <summary>Lấy hồ sơ của chính mình</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _service.GetProfileAsync(GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>Cập nhật hồ sơ cá nhân</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await _service.UpdateProfileAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    // ── Addresses ─────────────────────────────────────────

    /// <summary>Lấy danh sách địa chỉ giao hàng</summary>
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), 200)]
    public async Task<IActionResult> GetAddresses()
    {
        var result = await _service.GetAddressesAsync(GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>Lấy chi tiết một địa chỉ</summary>
    [HttpGet("me/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(AddressDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAddress(Guid addressId)
    {
        var result = await _service.GetAddressByIdAsync(GetCurrentUserId(), addressId);
        return Ok(result);
    }

    /// <summary>Thêm địa chỉ mới (tối đa 10)</summary>
    [HttpPost("me/addresses")]
    [ProducesResponseType(typeof(AddressDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
    {
        var result = await _service.CreateAddressAsync(GetCurrentUserId(), request);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật địa chỉ</summary>
    [HttpPut("me/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(AddressDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressRequest request)
    {
        var result = await _service.UpdateAddressAsync(GetCurrentUserId(), addressId, request);
        return Ok(result);
    }

    /// <summary>Xóa địa chỉ</summary>
    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        await _service.DeleteAddressAsync(GetCurrentUserId(), addressId);
        return NoContent();
    }

    /// <summary>Đặt địa chỉ làm mặc định</summary>
    [HttpPatch("me/addresses/{addressId:guid}/default")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetDefault(Guid addressId)
    {
        await _service.SetDefaultAddressAsync(GetCurrentUserId(), addressId);
        return Ok(new { message = "Đã đặt làm địa chỉ mặc định." });
    }

    // ── Sessions ──────────────────────────────────────────

    /// <summary>Xem danh sách thiết bị đang đăng nhập</summary>
    [HttpGet("me/sessions")]
    [ProducesResponseType(typeof(IEnumerable<SessionDto>), 200)]
    public async Task<IActionResult> GetSessions()
    {
        var result = await _service.GetActiveSessionsAsync(GetCurrentUserId());
        return Ok(result);
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
