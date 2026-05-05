using BookStore.Domain.Entities;
using BookStore.Domain.Enums;

namespace BookStore.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByCodeAsync(string orderCode);
    Task<Order?> GetDetailAsync(Guid id);
    Task<Order?> GetDetailByCodeAsync(string orderCode);
    Task<(IEnumerable<Order> Items, int Total)> GetPagedAsync(OrderFilter filter);
    Task<(IEnumerable<Order> Items, int Total)> GetByUserAsync(Guid userId, OrderFilter filter);
    Task<IEnumerable<Order>> GetPendingExpiredAsync(DateTime before);
    Task AddAsync(Order order);
    void Update(Order order);
    Task<int> SaveChangesAsync();
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByIdempotencyKeyAsync(string key);
    Task<IEnumerable<Payment>> GetByOrderAsync(Guid orderId);
    Task AddAsync(Payment payment);
    void Update(Payment payment);
    Task<int> SaveChangesAsync();
}

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id);
    Task<Shipment?> GetByOrderAsync(Guid orderId);
    Task<Shipment?> GetByTrackingAsync(string trackingNumber);
    Task AddAsync(Shipment shipment);
    void Update(Shipment shipment);
    Task<int> SaveChangesAsync();
}

public interface IRefundRepository
{
    Task<RefundRequest?> GetByOrderAsync(Guid orderId);
    Task<(IEnumerable<RefundRequest> Items, int Total)> GetPagedAsync(int page, int pageSize, RefundStatus? status);
    Task AddAsync(RefundRequest refund);
    void Update(RefundRequest refund);
    Task<int> SaveChangesAsync();
}

// ── Filter model ──────────────────────────────────────────
public class OrderFilter
{
    public string? Keyword { get; set; }
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
