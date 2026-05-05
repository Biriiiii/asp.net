using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

// ── SupplierRepository ────────────────────────────────────
public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _db;
    public SupplierRepository(AppDbContext db) => _db = db;

    public async Task<Supplier?> GetByIdAsync(Guid id) =>
        await _db.Suppliers.FindAsync(id);

    public async Task<(IEnumerable<Supplier> Items, int Total)> GetPagedAsync(
        string? keyword, int page, int pageSize)
    {
        var q = _db.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(s =>
                s.Name.ToLower().Contains(kw) ||
                (s.Phone    != null && s.Phone.Contains(kw)) ||
                (s.Email    != null && s.Email.ToLower().Contains(kw)) ||
                (s.TaxCode  != null && s.TaxCode.Contains(kw)));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(Supplier s) => await _db.Suppliers.AddAsync(s);
    public void Update(Supplier s)         => _db.Suppliers.Update(s);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── PurchaseOrderRepository ───────────────────────────────
public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _db;
    public PurchaseOrderRepository(AppDbContext db) => _db = db;

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id) =>
        await _db.PurchaseOrders.FindAsync(id);

    public async Task<PurchaseOrder?> GetDetailAsync(Guid id) =>
        await _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.Id == id);

    public async Task<(IEnumerable<PurchaseOrder> Items, int Total)> GetPagedAsync(
        string? status, int page, int pageSize)
    {
        var q = _db.PurchaseOrders
            .Include(po => po.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(po => po.Status == status);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(po => po.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(PurchaseOrder po) => await _db.PurchaseOrders.AddAsync(po);
    public void Update(PurchaseOrder po)          => _db.PurchaseOrders.Update(po);
    public async Task<int> SaveChangesAsync()     => await _db.SaveChangesAsync();
}
