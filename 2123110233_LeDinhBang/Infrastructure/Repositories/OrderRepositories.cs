using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

// ── OrderRepository ───────────────────────────────────────
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<Order?> GetByIdAsync(Guid id) => await _db.Orders.FindAsync(id);

    public async Task<Order?> GetByCodeAsync(string code) =>
        await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == code);

    public async Task<Order?> GetDetailAsync(Guid id) =>
        await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Include(o => o.Shipments).ThenInclude(s => s.TrackingEvents)
            .Include(o => o.StatusLogs.OrderBy(l => l.CreatedAt))
            .Include(o => o.RefundRequest)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order?> GetDetailByCodeAsync(string code) =>
        await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Include(o => o.Shipments).ThenInclude(s => s.TrackingEvents)
            .Include(o => o.StatusLogs)
            .Include(o => o.RefundRequest)
            .FirstOrDefaultAsync(o => o.OrderCode == code);

    public async Task<(IEnumerable<Order> Items, int Total)> GetPagedAsync(OrderFilter f)
    {
        var q     = BuildQuery(f);
        var total = await q.CountAsync();
        var items = await q.Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<Order> Items, int Total)> GetByUserAsync(Guid userId, OrderFilter f)
    {
        var q     = BuildQuery(f).Where(o => o.UserId == userId);
        var total = await q.CountAsync();
        var items = await q.Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<IEnumerable<Order>> GetPendingExpiredAsync(DateTime before) =>
        await _db.Orders.Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < before)
            .ToListAsync();

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);
    public void Update(Order order)          => _db.Orders.Update(order);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    private IQueryable<Order> BuildQuery(OrderFilter f)
    {
        var q = _db.Orders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var kw = f.Keyword.Trim().ToLower();
            q = q.Where(o => o.OrderCode.Contains(kw) ||
                              o.ShippingRecipientName.ToLower().Contains(kw) ||
                              o.ShippingPhone.Contains(kw));
        }
        if (f.Status.HasValue)        q = q.Where(o => o.Status        == f.Status.Value);
        if (f.PaymentStatus.HasValue) q = q.Where(o => o.PaymentStatus == f.PaymentStatus.Value);
        if (f.PaymentMethod.HasValue) q = q.Where(o => o.PaymentMethod == f.PaymentMethod.Value);
        if (f.FromDate.HasValue)      q = q.Where(o => o.CreatedAt >= f.FromDate.Value);
        if (f.ToDate.HasValue)        q = q.Where(o => o.CreatedAt <= f.ToDate.Value);
        return q;
    }
}

// ── PaymentRepository ─────────────────────────────────────
public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;
    public PaymentRepository(AppDbContext db) => _db = db;

    public async Task<Payment?> GetByIdAsync(Guid id) => await _db.Payments.FindAsync(id);

    public async Task<Payment?> GetByIdempotencyKeyAsync(string key) =>
        await _db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key);

    public async Task<IEnumerable<Payment>> GetByOrderAsync(Guid orderId) =>
        await _db.Payments.Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt).AsNoTracking().ToListAsync();

    public async Task AddAsync(Payment p) => await _db.Payments.AddAsync(p);
    public void Update(Payment p)         => _db.Payments.Update(p);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── ShipmentRepository ────────────────────────────────────
public class ShipmentRepository : IShipmentRepository
{
    private readonly AppDbContext _db;
    public ShipmentRepository(AppDbContext db) => _db = db;

    public async Task<Shipment?> GetByIdAsync(Guid id) =>
        await _db.Shipments.Include(s => s.TrackingEvents).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Shipment?> GetByOrderAsync(Guid orderId) =>
        await _db.Shipments.Include(s => s.TrackingEvents.OrderByDescending(e => e.OccurredAt))
            .FirstOrDefaultAsync(s => s.OrderId == orderId);

    public async Task<Shipment?> GetByTrackingAsync(string trackingNumber) =>
        await _db.Shipments.Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

    public async Task AddAsync(Shipment s) => await _db.Shipments.AddAsync(s);
    public void Update(Shipment s)         => _db.Shipments.Update(s);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── RefundRepository ──────────────────────────────────────
public class RefundRepository : IRefundRepository
{
    private readonly AppDbContext _db;
    public RefundRepository(AppDbContext db) => _db = db;

    public async Task<RefundRequest?> GetByOrderAsync(Guid orderId) =>
        await _db.RefundRequests.FirstOrDefaultAsync(r => r.OrderId == orderId);

    public async Task<(IEnumerable<RefundRequest> Items, int Total)> GetPagedAsync(
        int page, int pageSize, RefundStatus? status)
    {
        var q     = _db.RefundRequests.Include(r => r.Order).AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(RefundRequest r) => await _db.RefundRequests.AddAsync(r);
    public void Update(RefundRequest r)          => _db.RefundRequests.Update(r);
    public async Task<int> SaveChangesAsync()    => await _db.SaveChangesAsync();
}
