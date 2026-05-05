// ═══════════════════════════════════════════════════════════
// FILE: Promotion/API/Controllers/PromotionControllers.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Application.DTOs.Promotion;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/vouchers")]
[Produces("application/json")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _service;
    public VouchersController(IVoucherService service) => _service = service;

    /// <summary>Kiểm tra voucher có hợp lệ không (customer)</summary>
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(VoucherValidateDto), 200)]
    public async Task<IActionResult> Validate([FromBody] ValidateVoucherRequest request)
    {
        var userId = GetUserId();
        var result = await _service.ValidateAsync(request.Code, userId, request.Subtotal);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
        return Guid.Parse(userIdClaim);
    }

    /// <summary>Danh sách voucher [Admin/Marketing]</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(PagedResult<VoucherDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        try 
        {
            var result = await _service.GetPagedAsync(page, pageSize, isActive);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    /// <summary>Chi tiết voucher [Admin/Marketing]</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(VoucherDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>Tạo voucher [Admin/Marketing]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(VoucherDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateVoucherRequest request)
    {
        var result = await _service.CreateAsync(request);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật voucher [Admin/Marketing]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(VoucherDto), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateVoucherRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>Vô hiệu hóa voucher [Admin]</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok(new { message = "Voucher đã bị vô hiệu hóa." });
    }
}

[ApiController]
[Route("api/flash-sales")]
[Produces("application/json")]
public class FlashSalesController : ControllerBase
{
    private readonly IFlashSaleService _service;
    public FlashSalesController(IFlashSaleService service) => _service = service;

    /// <summary>Lấy flash sale đang diễn ra (hiển thị trang chủ)</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(FlashSaleDto), 200)]
    public async Task<IActionResult> GetActive()
        => Ok(await _service.GetActiveAsync());

    /// <summary>Danh sách flash sale [Admin/Marketing]</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(PagedResult<FlashSaleDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _service.GetPagedAsync(page, pageSize));

    /// <summary>Chi tiết flash sale [Admin/Marketing]</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(FlashSaleDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>Tạo flash sale [Admin/Marketing]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(FlashSaleDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateFlashSaleRequest request)
    {
        var result = await _service.CreateAsync(request);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật flash sale [Admin/Marketing]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Marketing")]
    [ProducesResponseType(typeof(FlashSaleDto), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateFlashSaleRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>Vô hiệu hóa flash sale [Admin]</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok(new { message = "Flash sale đã bị vô hiệu hóa." });
    }
}
