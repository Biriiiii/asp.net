using BookStore.Application.DTOs.Cart;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/cart")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _service;
    private const string SessionCookie  = "cart_session";
    private const string SessionHeader  = "X-Session-Id";

    public CartController(ICartService service) => _service = service;

    /// <summary>Xem giỏ hàng (guest hoặc đã đăng nhập)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), 200)]
    public async Task<IActionResult> GetCart()
    {
        var result = await _service.GetCartAsync(GetUserId(), GetSessionId());
        return Ok(result);
    }

    /// <summary>Thêm sản phẩm vào giỏ</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        EnsureSession();
        var result = await _service.AddItemAsync(GetUserId(), GetSessionId(), request);
        return Ok(result);
    }

    /// <summary>Cập nhật số lượng sản phẩm trong giỏ</summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequest request)
    {
        var result = await _service.UpdateItemAsync(GetUserId(), GetSessionId(), itemId, request);
        return Ok(result);
    }

    /// <summary>Xóa 1 sản phẩm khỏi giỏ</summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveItem(Guid itemId)
    {
        var result = await _service.RemoveItemAsync(GetUserId(), GetSessionId(), itemId);
        return Ok(result);
    }

    /// <summary>Xóa toàn bộ giỏ hàng</summary>
    [HttpDelete]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ClearCart()
    {
        await _service.ClearCartAsync(GetUserId(), GetSessionId());
        return Ok(new { message = "Đã xóa giỏ hàng." });
    }

    /// <summary>
    /// Merge giỏ guest vào user sau khi đăng nhập.
    /// Truyền sessionId qua header X-Session-Id hoặc cookie cart_session.
    /// </summary>
    [HttpPost("merge")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Merge()
    {
        var sessionId = GetSessionId();
        var userId    = GetUserId();

        if (sessionId == null || userId == null)
            return BadRequest(new { message = "Không đủ thông tin để merge." });

        await _service.MergeGuestCartAsync(sessionId, userId.Value);
        return Ok(new { message = "Merge giỏ hàng thành công." });
    }

    // ── Helpers ───────────────────────────────────────────

    private Guid? GetUserId()
    {
        // Thử đọc từ JWT claims
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetSessionId()
    {
        // Ưu tiên 1: Header X-Session-Id (dùng được trên Swagger)
        if (Request.Headers.TryGetValue(SessionHeader, out var headerVal) &&
            !string.IsNullOrWhiteSpace(headerVal))
            return headerVal.ToString();

        // Ưu tiên 2: Cookie cart_session (dùng trên browser)
        if (Request.Cookies.TryGetValue(SessionCookie, out var cookieVal) &&
            !string.IsNullOrWhiteSpace(cookieVal))
            return cookieVal;

        return null;
    }

    private void EnsureSession()
    {
        // Nếu đã login → dùng userId, không cần session
        if (GetUserId() != null) return;

        // Nếu chưa login → kiểm tra có sessionId chưa
        if (GetSessionId() != null) return;

        // Nếu không có cả hai → tạo session mới qua cookie
        var newSession = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(SessionCookie, newSession, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}