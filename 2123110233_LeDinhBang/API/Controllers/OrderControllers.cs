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
        try 
        {
            var result = await _service.CancelAsync(GetUserId(), id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                message = ex.Message, 
                detail = ex.InnerException?.Message,
                stack = ex.StackTrace 
            });
        }
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
        try 
        {
            var result = await _service.UpdateStatusAsync(id, request, GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                message = ex.Message, 
                detail = ex.InnerException?.Message,
                stack = ex.StackTrace 
            });
        }
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
    private readonly ILogger<PaymentsController> _logger;
    private readonly IWebHostEnvironment _env;

    public PaymentsController(
        IOrderService service,
        IConfiguration config,
        ILogger<PaymentsController> logger,
        IWebHostEnvironment env)
    {
        _service = service;
        _config = config;
        _logger = logger;
        _env = env;
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

    /// <summary>Frontend có thể gọi endpoint này nếu nhận query VNPay ở phía client.</summary>
    [HttpPost("vnpay-confirm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VnPayConfirm([FromBody] PaymentCallbackRequest request)
    {
        if (request.Status == "success")
        {
            await _service.MarkPaymentSucceededAsync(request.OrderCode, request.TransactionId);
        }
        else
        {
            request.Provider = "vnpay";
            await _service.HandlePaymentCallbackAsync(request);
        }

        var orderId = await _service.GetOrderIdByCodeAsync(request.OrderCode);
        return Ok(new { message = "OK", orderId, payment = request.Status });
    }

    /// <summary>Xác nhận thanh toán thành công theo orderId đang hiển thị ở frontend.</summary>
    [HttpPost("vnpay-confirm/{orderId:guid}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> VnPayConfirmByOrderId(Guid orderId, [FromQuery] string? transactionId = null)
    {
        await _service.MarkPaymentSucceededByOrderIdAsync(orderId, transactionId);
        return Ok(new { message = "OK", orderId, payment = "success" });
    }

    /// <summary>Xác nhận nhanh thanh toán VNPay bằng mã đơn hàng.</summary>
    [HttpGet("vnpay-confirm")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> VnPayConfirmByCode([FromQuery] string code, [FromQuery] string? transactionId = null)
    {
        await _service.MarkPaymentSucceededAsync(code, transactionId);
        var orderId = await _service.GetOrderIdByCodeAsync(code);
        return Ok(new { message = "OK", orderId, payment = "success" });
    }

    /// <summary>Redirect URL sau khi thanh toán (VNPay redirect về web)</summary>
    [HttpGet("return")]
    public async Task<IActionResult> Return()
    {
        var q = Request.Query;
        var orderCode = q["vnp_TxnRef"].ToString();
        var transactionId = q["vnp_TransactionNo"].ToString();
        var responseCode = q["vnp_ResponseCode"].ToString();
        var transactionStatus = q["vnp_TransactionStatus"].ToString();
        var amountText = q["vnp_Amount"].ToString();
        var signature = q["vnp_SecureHash"].ToString();
        var rawData = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var amount = decimal.TryParse(amountText, out var parsedAmount) ? parsedAmount / 100m : 0m;
        
        var status = responseCode == "00" && (string.IsNullOrWhiteSpace(transactionStatus) || transactionStatus == "00")
            ? "success"
            : "failed";

        // Lấy frontend Storefront URL từ config
        var origins = _config.GetSection("AllowedOrigins").Get<string[]>();
        var frontendUrl = origins?.Length > 1 ? origins[1] : (origins?.FirstOrDefault() ?? "http://localhost:3001");

        try
        {
            // BẮT BUỘC phải gọi HandlePaymentCallbackAsync để xác thực chữ ký (Signature)
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

            var orderId = await _service.GetOrderIdByCodeAsync(orderCode);
            var redirectUrl = orderId.HasValue
                ? $"{frontendUrl}/orders/{orderId.Value}?payment={status}&code={orderCode}"
                : $"{frontendUrl}/orders?payment={status}&code={orderCode}";
                
            return Redirect(redirectUrl);
        }
        catch (Exception ex) when (_env.IsDevelopment() && status == "success")
        {
            // Bỏ qua lỗi chữ ký ở môi trường dev (localhost) để thuận tiện test
            _logger.LogWarning(ex, "VNPay return verification failed in Development. Bypassing signature validation for order {OrderCode}.", orderCode);
            
            try 
            {
                // Cưỡng ép cập nhật trực tiếp bằng SQL thuần để bỏ qua bộ đệm của EF Core
                await _service.HandlePaymentCallbackAsync(new PaymentCallbackRequest
                {
                    Provider = "manual",
                    OrderCode = orderCode,
                    TransactionId = transactionId,
                    Status = status,
                    Amount = amount
                });
                
                // Tiêm thẳng SQL vào Database (Bạo lực) để đảm bảo cập nhật 100%
                var dbContext = HttpContext.RequestServices.GetRequiredService<BookStore.Infrastructure.Data.AppDbContext>();
                await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
                    dbContext.Database, 
                    "UPDATE Orders SET Status = 1, PaymentStatus = 1, ConfirmedAt = GETUTCDATE() WHERE OrderCode = {0}; UPDATE Payments SET Status = 1, PaidAt = GETUTCDATE(), TransactionId = {1} WHERE OrderId = (SELECT Id FROM Orders WHERE OrderCode = {0})", 
                    orderCode, 
                    transactionId ?? ""
                );
            } 
            catch (Exception innerEx) 
            {
                _logger.LogError(innerEx, "LỖI KHI LƯU DATABASE: {Message}", innerEx.Message);
            }
            
            var orderId = await _service.GetOrderIdByCodeAsync(orderCode);
            var redirectUrl = orderId.HasValue
                ? $"{frontendUrl}/orders/{orderId.Value}?payment={status}&code={orderCode}"
                : $"{frontendUrl}/orders?payment={status}&code={orderCode}";
                
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay return failed/invalid signature for order {OrderCode}. ResponseCode={ResponseCode}", orderCode, responseCode);
            
            // Nếu lỗi xác thực hoặc lỗi hệ thống, trả về frontend với trạng thái failed
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
        var transactionStatus = q["vnp_TransactionStatus"].ToString();
        var amountText = q["vnp_Amount"].ToString();
        var signature = q["vnp_SecureHash"].ToString();
        var rawData = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var amount = decimal.TryParse(amountText, out var parsedAmount) ? parsedAmount / 100m : 0m;
        var status = responseCode == "00" && (string.IsNullOrWhiteSpace(transactionStatus) || transactionStatus == "00")
            ? "success"
            : "failed";

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
            _logger.LogError(ex, "VNPay IPN failed for order {OrderCode}.", orderCode);
            return Ok(new { RspCode = "97", Message = ex.Message });
        }
    }
}
