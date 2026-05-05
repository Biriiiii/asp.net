// ═══════════════════════════════════════════════════════════
// FILE: Review/Infrastructure/Repositories/ReviewRepository.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public async Task<Review?> GetByIdAsync(Guid id) =>
        await _db.Reviews.Include(r => r.Images).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Review?> GetByUserAndProductAsync(Guid userId, Guid productId) =>
        await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<(IEnumerable<Review> Items, int Total)> GetByProductAsync(
        Guid productId, int page, int pageSize, string sort)
    {
        var q = _db.Reviews.Include(r => r.Images)
            .Where(r => r.ProductId == productId && r.IsVisible);

        q = sort switch
        {
            "helpful" => q.OrderByDescending(r => r.HelpfulCount),
            "highest" => q.OrderByDescending(r => r.Rating),
            "lowest"  => q.OrderBy(r => r.Rating),
            _         => q.OrderByDescending(r => r.CreatedAt)
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<bool> HasPurchasedAndDeliveredAsync(Guid userId, Guid productId) =>
        await _db.Orders.AnyAsync(o =>
            o.UserId == userId &&
            o.Status == Domain.Enums.OrderStatus.Delivered &&
            o.Items.Any(i => i.ProductId == productId));

    public async Task AddAsync(Review r) => await _db.Reviews.AddAsync(r);
    public void Update(Review r)         => _db.Reviews.Update(r);
    public void Delete(Review r)         => _db.Reviews.Remove(r);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}
