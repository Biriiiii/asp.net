// ═══════════════════════════════════════════════════════════
// FILE: Promotion/Infrastructure/Repositories/PromotionRepositories.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

public class VoucherRepository : IVoucherRepository
{
    private readonly AppDbContext _db;
    public VoucherRepository(AppDbContext db) => _db = db;

    public async Task<Voucher?> GetByIdAsync(Guid id) => await _db.Vouchers.FindAsync(id);
    public async Task<Voucher?> GetByCodeAsync(string code) =>
        await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == code.ToUpper());

    public async Task<(IEnumerable<Voucher> Items, int Total)> GetPagedAsync(int page, int pageSize, bool? isActive)
    {
        var q = _db.Vouchers.AsQueryable();
        if (isActive.HasValue) q = q.Where(v => v.IsActive == isActive.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<int> GetUserUsageCountAsync(Guid voucherId, Guid userId) =>
        await _db.VoucherUsages.CountAsync(u => u.VoucherId == voucherId && u.UserId == userId);

    public async Task IncrementUsageAsync(Guid voucherId, Guid userId, Guid orderId, decimal discountApplied)
    {
        var v = await _db.Vouchers.FindAsync(voucherId);
        if (v != null) { v.UsedCount++; _db.Vouchers.Update(v); }
        await _db.VoucherUsages.AddAsync(new VoucherUsage
        {
            VoucherId = voucherId, UserId = userId,
            OrderId = orderId, DiscountApplied = discountApplied
        });
    }

    public async Task DecrementUsageAsync(Guid voucherId, Guid userId)
    {
        var v = await _db.Vouchers.FindAsync(voucherId);
        if (v != null) { v.UsedCount = Math.Max(0, v.UsedCount - 1); _db.Vouchers.Update(v); }
        var usage = await _db.VoucherUsages
            .FirstOrDefaultAsync(u => u.VoucherId == voucherId && u.UserId == userId);
        if (usage != null) _db.VoucherUsages.Remove(usage);
    }

    public async Task AddAsync(Voucher v) => await _db.Vouchers.AddAsync(v);
    public void Update(Voucher v)         => _db.Vouchers.Update(v);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

public class FlashSaleRepository : IFlashSaleRepository
{
    private readonly AppDbContext _db;
    public FlashSaleRepository(AppDbContext db) => _db = db;

    public async Task<FlashSale?> GetByIdAsync(Guid id) =>
        await _db.FlashSales.Include(f => f.Items)
            .ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<FlashSale?> GetActiveAsync() =>
        await _db.FlashSales.Include(f => f.Items)
            .ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(f => f.IsActive &&
                DateTime.UtcNow >= f.StartTime && DateTime.UtcNow <= f.EndTime);

    public async Task<(IEnumerable<FlashSale> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var total = await _db.FlashSales.CountAsync();
        var items = await _db.FlashSales.Include(f => f.Items)
            .OrderByDescending(f => f.StartTime)
            .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(FlashSale f) => await _db.FlashSales.AddAsync(f);
    public void Update(FlashSale f)         => _db.FlashSales.Update(f);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}
