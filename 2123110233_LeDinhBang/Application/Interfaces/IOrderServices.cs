using BookStore.Application.DTOs.Order;
using BookStore.Domain.Enums;

namespace BookStore.Application.Interfaces;

public interface IOrderService
{
    // Customer
    Task<CheckoutSummaryDto> PreviewAsync(Guid userId, CreateOrderRequest request);
    Task<OrderDetailDto> CreateAsync(Guid userId, string? sessionId, CreateOrderRequest request);
    Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(Guid userId, OrderQueryParams query);
    Task<OrderDetailDto> GetMyDetailAsync(Guid userId, Guid orderId);
    Task<OrderDetailDto> CancelAsync(Guid userId, Guid orderId, CancelOrderRequest request);
    Task<string> CreatePaymentUrlAsync(Guid userId, Guid orderId, string returnUrl);

    // Payment callback (webhook từ cổng TT)
    Task HandlePaymentCallbackAsync(PaymentCallbackRequest callback);

    // Admin / Staff
    Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryParams query);
    Task<OrderDetailDto> GetDetailAsync(Guid orderId);
    Task<OrderDetailDto> UpdateStatusAsync(Guid orderId, AdminUpdateOrderStatusRequest request, Guid staffId);
    Task<OrderDetailDto> AssignShipmentAsync(Guid orderId, UpdateShipmentRequest request, Guid staffId);
    Task<RefundDto> ProcessRefundAsync(Guid orderId, ProcessRefundRequest request, Guid adminId);
}

public interface IOrderCodeGenerator
{
    Task<string> GenerateAsync();   // DH + yyyyMMdd + 4-digit seq (thread-safe)
}

public interface IShippingFeeService
{
    Task<decimal> CalculateAsync(string province, ShippingMethod method, decimal weightGram);
}

public interface IInventoryReserveService
{
    Task<bool> CheckAvailabilityAsync(IEnumerable<(Guid ProductId, int Qty)> items);
    Task ReserveAsync(IEnumerable<(Guid ProductId, int Qty)> items);
    Task CommitAsync(IEnumerable<(Guid ProductId, int Qty)> items);
    Task ReleaseAsync(IEnumerable<(Guid ProductId, int Qty)> items);
}

public interface IPaymentGatewayService
{
    Task<string> CreatePaymentUrlAsync(string orderCode, decimal amount, PaymentMethod method, string returnUrl);
    bool VerifyCallback(PaymentCallbackRequest callback);
}
