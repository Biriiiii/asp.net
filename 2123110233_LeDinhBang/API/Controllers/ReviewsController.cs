// ═══════════════════════════════════════════════════════════
// FILE: Review/API/Controllers/ReviewsController.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Application.DTOs.Review;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;
    public ReviewsController(IReviewService service) => _service = service;

    /// <summary>Danh sách đánh giá theo sản phẩm</summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), 200)]
    public async Task<IActionResult> GetByProduct(Guid productId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string sort = "newest")
        => Ok(await _service.GetByProductAsync(productId, page, pageSize, sort));

    /// <summary>Tổng hợp sao + phân bố đánh giá</summary>
    [HttpGet("product/{productId:guid}/summary")]
    [ProducesResponseType(typeof(ReviewSummaryDto), 200)]
    public async Task<IActionResult> GetSummary(Guid productId)
        => Ok(await _service.GetSummaryAsync(productId));

    /// <summary>Viết đánh giá (chỉ sau khi đơn DELIVERED)</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), request);
        return StatusCode(201, result);
    }

    /// <summary>Chỉnh sửa đánh giá (trong vòng 7 ngày)</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequest request)
        => Ok(await _service.UpdateAsync(GetUserId(), id, request));

    /// <summary>Xóa đánh giá của mình</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    /// <summary>Vote helpful cho review</summary>
    [HttpPost("{id:guid}/helpful")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkHelpful(Guid id)
    {
        await _service.MarkHelpfulAsync(id);
        return Ok(new { message = "Cảm ơn phản hồi của bạn." });
    }

    /// <summary>Ẩn/hiện review [Admin/ContentManager]</summary>
    [HttpPatch("{id:guid}/visibility")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ToggleVisibility(Guid id)
    {
        await _service.ToggleVisibilityAsync(id);
        return Ok(new { message = "Đã cập nhật trạng thái hiển thị." });
    }

    /// <summary>Xóa review vi phạm [Admin/ContentManager]</summary>
    [HttpDelete("{id:guid}/admin")]
    [Authorize(Roles = "Admin,ContentManager")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AdminDelete(Guid id)
    {
        await _service.DeleteAsync(Guid.Empty, id, isAdmin: true);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
