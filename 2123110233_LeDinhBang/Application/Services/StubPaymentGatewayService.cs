using BookStore.Application.DTOs.Order;
using BookStore.Application.Interfaces;
using BookStore.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Services;

/// <summary>
/// Stub PaymentGatewayService — log ra console, chưa tích hợp VNPay/MoMo thật.
/// Thay bằng VNPayService / MoMoService khi go production.
/// </summary>
public class StubPaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StubPaymentGatewayService> _logger;
    private readonly IConfiguration _config;

    public StubPaymentGatewayService(
        ILogger<StubPaymentGatewayService> logger,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task<string> CreatePaymentUrlAsync(
        string orderCode, decimal amount,
        PaymentMethod method, string returnUrl)
    {
        _logger.LogInformation(
            "[PAYMENT] Tạo URL thanh toán: OrderCode={OrderCode}, Amount={Amount}, Method={Method}",
            orderCode, amount, method);

        // Stub: trả về URL giả lập, redirect thẳng về returnUrl
        // Thực tế: gọi VNPay/MoMo SDK để tạo URL thật
        var fakeUrl = $"{returnUrl}?provider={method}&orderCode={orderCode}&status=success&transactionId=FAKE_{Guid.NewGuid():N}";

        return Task.FromResult(fakeUrl);
    }

    public bool VerifyCallback(PaymentCallbackRequest callback)
    {
        _logger.LogInformation(
            "[PAYMENT] Xác minh callback: Provider={Provider}, OrderCode={OrderCode}, Status={Status}",
            callback.Provider, callback.OrderCode, callback.Status);

        // Stub: luôn trả true (bỏ qua kiểm tra chữ ký)
        // Thực tế: xác minh HMAC/SHA256 signature từ VNPay/MoMo
        if (string.IsNullOrWhiteSpace(callback.Signature))
        {
            _logger.LogWarning("[PAYMENT] Không có signature — stub mode, bỏ qua xác minh.");
            return true;
        }

        return true;
    }
}