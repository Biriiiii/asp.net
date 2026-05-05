using BookStore.Application.DTOs.Order;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Controllers;

// ── OrdersController (Customer) ───────────────────────────
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public OrdersController(IOrderService service) => _service = service;

    /// <summary>Xem trước tổng tiền trước khi đặt hàng</summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(CheckoutSummaryDto), 200)]
    public async Task<IActionResult> Preview([FromBody] CreateOrderRequest request)
    {
        var result = await _service.PreviewAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>Đặt hàng</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), GetSessionId(), request);
        return StatusCode(201, result);
    }

    private string? GetSessionId()
    {
        // Header X-Session-Id (Swagger) hoặc Cookie cart_session (Browser)
        if (Request.Headers.TryGetValue("X-Session-Id", out var h) && !string.IsNullOrWhiteSpace(h))
            return h.ToString();
        if (Request.Cookies.TryGetValue("cart_session", out var c) && !string.IsNullOrWhiteSpace(c))
            return c;
        return null;
    }

    /// <summary>Danh sách đơn hàng của tôi</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), 200)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParams query)
    {
        var result = await _service.GetMyOrdersAsync(GetUserId(), query);
        return Ok(result);
    }

    /// <summary>Chi tiết đơn hàng của tôi</summary>
    [HttpGet("my/{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyOrder(Guid id)
    {
        var result = await _service.GetMyDetailAsync(GetUserId(), id);
        return Ok(result);
    }

    /// <summary>Hủy đơn hàng (trong 2 giờ sau xác nhận)</summary>
    [HttpPost("my/{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request)
    {
        var result = await _service.CancelAsync(GetUserId(), id, request);
        return Ok(result);
    }


    /// <summary>Tạo URL thanh toán online cho đơn hàng của tôi</summary>
    [HttpPost("my/{id:guid}/payment-url")]
    [ProducesResponseType(typeof(PaymentUrlDto), 200)]
    public async Task<IActionResult> CreatePaymentUrl(Guid id)
    {
        var returnUrl = $"{Request.Scheme}://{Request.Host}/api/payments/return";
        var paymentUrl = await _service.CreatePaymentUrlAsync(GetUserId(), id, returnUrl);
        return Ok(new PaymentUrlDto(paymentUrl));
    }
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
        }
        return Guid.Parse(userIdClaim);
    }
}

// ── AdminOrdersController (Admin/Staff) ───────────────────
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public AdminOrdersController(IOrderService service) => _service = service;

    /// <summary>Danh sách tất cả đơn hàng</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] OrderQueryParams query)
        => Ok(await _service.GetAllAsync(query));

    /// <summary>Chi tiết đơn hàng</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetDetailAsync(id));

    /// <summary>Cập nhật trạng thái đơn hàng</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminUpdateOrderStatusRequest request)
    {
        // Đảm bảo dòng này đang gọi đúng service
        var result = await _service.UpdateStatusAsync(id, request, GetUserId());
        return Ok(result);
    }

    /// <summary>Gán mã vận đơn → chuyển sang Shipping</summary>
    [HttpPost("{id:guid}/shipment")]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AssignShipment(Guid id, [FromBody] UpdateShipmentRequest request)
        => Ok(await _service.AssignShipmentAsync(id, request, GetUserId()));

    /// <summary>Xử lý yêu cầu hoàn tiền [Admin only]</summary>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RefundDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] ProcessRefundRequest request)
        => Ok(await _service.ProcessRefundAsync(id, request, GetUserId()));

    private Guid GetUserId()
    {
        // Ưu tiên Sub (JWT standard), sau đó NameIdentifier (ASP.NET mapped)
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                       ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token.");
        return Guid.Parse(userIdClaim);
    }
}

// ── PaymentsController (Webhook) ──────────────────────────
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly IConfiguration _config;
    public PaymentsController(IOrderService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    /// <summary>
    /// Webhook nhận callback từ VNPay / MoMo / ZaloPay.
    /// Endpoint PUBLIC — bảo mật bằng signature verification bên trong service.
    /// </summary>
    [HttpPost("callback")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Callback([FromBody] PaymentCallbackRequest request)
    {
        await _service.HandlePaymentCallbackAsync(request);
        return Ok(new { message = "OK" });
    }

    /// <summary>Redirect URL sau khi thanh toán (VNPay redirect về web)</summary>
    [HttpGet("return")]
    public async Task<IActionResult> Return()
    {
        var q = Request.Query;
        var orderCode = q["vnp_TxnRef"].ToString();
        var transactionId = q["vnp_TransactionNo"].ToString();
        var responseCode = q["vnp_ResponseCode"].ToString();
        var amountText = q["vnp_Amount"].ToString();
        var signature = q["vnp_SecureHash"].ToString();
        var rawData = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var amount = decimal.TryParse(amountText, out var parsedAmount) ? parsedAmount / 100m : 0m;
        var status = responseCode == "00" ? "success" : "failed";

        // Lấy frontend Storefront URL từ config (AllowedOrigins[1] = localhost:3001)
        var origins = _config.GetSection("AllowedOrigins").Get<string[]>();
        var frontendUrl = origins?.Length > 1 ? origins[1] : (origins?.FirstOrDefault() ?? "http://localhost:3001");

        try
        {
            await _service.HandlePaymentCallbackAsync(new PaymentCallbackRequest
            {
                Provider = "vnpay",
                OrderCode = orderCode,
                TransactionId = transactionId,
                Status = status,
                Amount = amount,
                Signature = signature,
                RawData = rawData
            });
            return Redirect($"{frontendUrl}/orders?payment={status}&code={orderCode}");
        }
        catch
        {
            return Redirect($"{frontendUrl}/orders?payment=failed&code={orderCode}");
        }
    }

    /// <summary>VNPay IPN endpoint</summary>
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        var q = Request.Query;
        var orderCode = q["vnp_TxnRef"].ToString();
        var transactionId = q["vnp_TransactionNo"].ToString();
        var responseCode = q["vnp_ResponseCode"].ToString();
        var amountText = q["vnp_Amount"].ToString();
        var signature = q["vnp_SecureHash"].ToString();
        var rawData = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var amount = decimal.TryParse(amountText, out var parsedAmount) ? parsedAmount / 100m : 0m;
        var status = responseCode == "00" ? "success" : "failed";

        try
        {
            await _service.HandlePaymentCallbackAsync(new PaymentCallbackRequest
            {
                Provider = "vnpay",
                OrderCode = orderCode,
                TransactionId = transactionId,
                Status = status,
                Amount = amount,
                Signature = signature,
                RawData = rawData
            });
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }
        catch (Exception ex)
        {
            return Ok(new { RspCode = "97", Message = ex.Message });
        }
    }
}
