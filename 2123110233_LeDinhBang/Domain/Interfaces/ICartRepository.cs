using BookStore.Domain.Entities;

namespace BookStore.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserAsync(Guid userId);
    Task<Cart?> GetBySessionAsync(string sessionId);
    Task<Cart?> GetWithItemsAsync(Guid userId);
    Task<Cart?> GetWithItemsBySessionAsync(string sessionId);
    Task AddAsync(Cart cart);
    void Update(Cart cart);
    Task RemoveItemsAsync(Guid cartId, IEnumerable<Guid> itemIds);
    Task<int> SaveChangesAsync();
}
