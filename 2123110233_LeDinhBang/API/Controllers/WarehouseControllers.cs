using BookStore.Application.DTOs.Warehouse;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

// ── SuppliersController ───────────────────────────────────
[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;
    public SuppliersController(ISupplierService service) => _service = service;

    /// <summary>Danh sách nhà cung cấp có tìm kiếm</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _service.GetPagedAsync(keyword, page, pageSize));

    /// <summary>Chi tiết nhà cung cấp</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>Thêm nhà cung cấp mới [Admin]</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupplierDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        var result = await _service.CreateAsync(request);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật nhà cung cấp [Admin]</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SupplierDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSupplierRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    /// <summary>Vô hiệu hóa nhà cung cấp [Admin]</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok(new { message = "Nhà cung cấp đã bị vô hiệu hóa." });
    }
}

// ── PurchaseOrdersController ──────────────────────────────
[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    public PurchaseOrdersController(IPurchaseOrderService service) => _service = service;

    /// <summary>Danh sách phiếu nhập hàng</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseOrderDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _service.GetPagedAsync(status, page, pageSize));

    /// <summary>Chi tiết phiếu nhập hàng</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    /// <summary>Tạo phiếu nhập hàng mới [Admin/Staff]</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var createdBy = GetUserId();
        var result    = await _service.CreateAsync(createdBy, request);
        return StatusCode(201, result);
    }

    /// <summary>Phê duyệt phiếu nhập [Admin]</summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id)
        => Ok(await _service.ApproveAsync(id, GetUserId()));

    /// <summary>Nhận hàng thực tế — cộng vào tồn kho [Staff]</summary>
    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceivePurchaseOrderRequest request)
        => Ok(await _service.ReceiveAsync(id, request));

    /// <summary>Hủy phiếu nhập [Admin]</summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id)
        => Ok(await _service.CancelAsync(id));

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
