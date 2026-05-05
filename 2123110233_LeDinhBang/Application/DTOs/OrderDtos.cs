using BookStore.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Order;

// ── Responses ─────────────────────────────────────────────

public record OrderListItemDto(
    Guid Id, string OrderCode, string Status,
    string RecipientName, string Phone,
    string PaymentMethod, string PaymentStatus,
    int ItemCount, decimal TotalAmount,
    DateTime CreatedAt, DateTime? ConfirmedAt
);

public record OrderDetailDto(
    Guid Id, string OrderCode, string Status, bool CanCancel,
    ShippingAddressDto ShippingAddress,
    string ShippingMethod, decimal ShippingFee,
    decimal Subtotal, decimal DiscountAmount, decimal TotalAmount,
    string PaymentMethod, string PaymentStatus,
    string? Note, string? CancelReason,
    IEnumerable<OrderItemDto> Items,
    IEnumerable<PaymentDto> Payments,
    ShipmentDto? Shipment,
    IEnumerable<OrderStatusLogDto> StatusLogs,
    RefundDto? Refund,
    DateTime CreatedAt, DateTime? ConfirmedAt, DateTime? CancelledAt
);

public record ShippingAddressDto(
    string RecipientName, string Phone,
    string Province, string District, string Ward, string AddressLine
);

public record OrderItemDto(
    Guid Id, Guid ProductId,
    string Title, string? Isbn, string? CoverUrl, string? AuthorNames,
    int Quantity, decimal UnitPrice, decimal DiscountAmount, decimal LineTotal
);

public record PaymentDto(
    Guid Id, string Provider, string? TransactionId,
    decimal Amount, string Status, string? FailureReason,
    DateTime? PaidAt, DateTime? ExpiredAt, DateTime CreatedAt
);

public record ShipmentDto(
    Guid Id, string Carrier, string TrackingNumber, string Status,
    DateTime? ShippedAt, DateTime? EstimatedDelivery, DateTime? DeliveredAt,
    IEnumerable<TrackingEventDto> Events
);

public record TrackingEventDto(
    string Status, string? Description, string? Location, DateTime OccurredAt
);

public record OrderStatusLogDto(
    string FromStatus, string ToStatus, string? Note, DateTime CreatedAt
);

public record RefundDto(
    Guid Id, decimal Amount, string Reason, string Status,
    string? AdminNote, string? TransactionId,
    DateTime? ProcessedAt, DateTime CreatedAt
);

public record CheckoutSummaryDto(
    decimal Subtotal, decimal ShippingFee,
    decimal DiscountAmount, decimal TotalAmount,
    string? VoucherCode, decimal VoucherDiscount
);

public record PaymentUrlDto(string PaymentUrl);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext  => Page < TotalPages;
    public bool HasPrev  => Page > 1;
}

// ── Requests ──────────────────────────────────────────────

public class CreateOrderRequest
{
    [Required] public ShippingAddressRequest ShippingAddress { get; set; } = new();
    [Required] public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
    [Required] public PaymentMethod PaymentMethod { get; set; }
    public string? VoucherCode { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public List<Guid>? CartItemIds { get; set; }   // null = lấy toàn bộ giỏ
}

public class ShippingAddressRequest
{
    [Required][MaxLength(200)] public string RecipientName { get; set; } = string.Empty;
    [Required][Phone][MaxLength(15)] public string Phone { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string Province { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string District { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string Ward { get; set; } = string.Empty;
    [Required][MaxLength(500)] public string AddressLine { get; set; } = string.Empty;
}

public class CancelOrderRequest
{
    [Required][MaxLength(500)] public string Reason { get; set; } = string.Empty;
}

public class UpdateShipmentRequest
{
    [Required][MaxLength(50)]  public string Carrier { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string TrackingNumber { get; set; } = string.Empty;
    public DateTime? EstimatedDelivery { get; set; }
}

public class ProcessRefundRequest
{
    [Required] public bool Approved { get; set; }
    [MaxLength(500)] public string? AdminNote { get; set; }
    [MaxLength(100)] public string? TransactionId { get; set; }
}

public class AdminUpdateOrderStatusRequest
{
    [Required] public OrderStatus Status { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}

public class OrderQueryParams
{
    public string? Keyword { get; set; }
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public class PaymentCallbackRequest
{
    [Required] public string Provider { get; set; } = string.Empty;
    [Required] public string OrderCode { get; set; } = string.Empty;
    [Required] public string TransactionId { get; set; } = string.Empty;
    [Required] public string Status { get; set; } = string.Empty;  // success | failed
    public decimal Amount { get; set; }
    public string? RawData { get; set; }
    public string? Signature { get; set; }
}
