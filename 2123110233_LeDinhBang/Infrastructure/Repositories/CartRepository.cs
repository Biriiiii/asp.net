using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public async Task<Cart?> GetByUserAsync(Guid userId) =>
        await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Cart?> GetBySessionAsync(string sessionId) =>
        await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

    public async Task<Cart?> GetWithItemsAsync(Guid userId) =>
        await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductAuthors)
                        .ThenInclude(pa => pa.Author)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Inventory)
            .FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Cart?> GetWithItemsBySessionAsync(string sessionId) =>
        await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Inventory)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

    public async Task AddAsync(Cart cart) => await _db.Carts.AddAsync(cart);
    public void Update(Cart cart)         => _db.Carts.Update(cart);

    public async Task RemoveItemsAsync(Guid cartId, IEnumerable<Guid> itemIds)
    {
        var items = await _db.CartItems
            .Where(i => i.CartId == cartId && itemIds.Contains(i.Id))
            .ToListAsync();
        _db.CartItems.RemoveRange(items);
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}
