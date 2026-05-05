using BookStore.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Domain.Entities;

/// <summary>Đơn hàng — snapshot toàn bộ địa chỉ + giá tại thời điểm đặt</summary>
public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public string OrderCode { get; set; } = string.Empty;           // DH20240401_0001
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // ── Snapshot địa chỉ giao hàng ────────────────────────
    public string ShippingRecipientName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingAddressLine { get; set; } = string.Empty;

    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
    public decimal ShippingFee { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public Guid? VoucherId { get; set; }
    public string? CancelReason { get; set; }
    public string? Note { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // ── Navigation ─────────────────────────────────────────
    public ICollection<OrderItem>      Items      { get; set; } = new List<OrderItem>();
    public ICollection<Payment>        Payments   { get; set; } = new List<Payment>();
    public ICollection<Shipment>       Shipments  { get; set; } = new List<Shipment>();
    public ICollection<OrderStatusLog> StatusLogs { get; set; } = new List<OrderStatusLog>();
    public RefundRequest? RefundRequest { get; set; }

    // ── Computed (không lưu DB) ────────────────────────────
    [NotMapped]
    public bool CanCancel =>
        Status == OrderStatus.Pending ||
        (Status == OrderStatus.Confirmed &&
         ConfirmedAt.HasValue &&
         DateTime.UtcNow - ConfirmedAt.Value < TimeSpan.FromHours(2));
}

/// <summary>Item trong đơn hàng — snapshot tên/giá/ảnh sản phẩm tại thời điểm mua</summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }

    // Snapshot sản phẩm (tránh sai lệch nếu sản phẩm thay đổi sau này)
    public string SnapshotTitle { get; set; } = string.Empty;
    public string? SnapshotIsbn { get; set; }
    public string? SnapshotCoverUrl { get; set; }
    public string? SnapshotAuthorNames { get; set; }

    public Order Order { get; set; } = null!;

    [NotMapped] public decimal LineTotal => (UnitPrice - DiscountAmount) * Quantity;
}

/// <summary>Thanh toán — IdempotencyKey chống double-charge</summary>
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = string.Empty;    // vnpay | momo | zalopay | cod
    public string? TransactionId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;   // Chống thanh toán trùng
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    public string? ProviderResponse { get; set; }           // JSON raw từ cổng TT
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiredAt { get; set; }                // Timeout PENDING

    public Order Order { get; set; } = null!;
}

/// <summary>Vận đơn — tracking từng bước từ carrier</summary>
public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Carrier { get; set; } = string.Empty;    // GHN | GHTK
    public string TrackingNumber { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public string? CarrierStatus { get; set; }              // Raw status từ carrier
    public DateTime? ShippedAt { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<ShipmentTracking> TrackingEvents { get; set; } = new List<ShipmentTracking>();
}

/// <summary>Lịch sử tracking từng checkpoint của vận đơn</summary>
public class ShipmentTracking : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime OccurredAt { get; set; }

    public Shipment Shipment { get; set; } = null!;
}

/// <summary>Audit trail — lưu toàn bộ thay đổi trạng thái đơn hàng</summary>
public class OrderStatusLog : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public Guid? ChangedBy { get; set; }    // null = system tự động

    public Order Order { get; set; } = null!;
}

/// <summary>Yêu cầu hoàn tiền — quan hệ 1-1 với Order</summary>
public class RefundRequest : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? AdminNote { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedBy { get; set; }

    public Order Order { get; set; } = null!;
}
