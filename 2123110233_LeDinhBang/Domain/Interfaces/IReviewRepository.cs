// ═══════════════════════════════════════════════════════════
// FILE: Review/Domain/Interfaces/IReviewRepository.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Domain.Entities;

namespace BookStore.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review?> GetByUserAndProductAsync(Guid userId, Guid productId);
    Task<(IEnumerable<Review> Items, int Total)> GetByProductAsync(Guid productId, int page, int pageSize, string sort);
    Task<bool> HasPurchasedAndDeliveredAsync(Guid userId, Guid productId);
    Task AddAsync(Review review);
    void Update(Review review);
    void Delete(Review review);
    Task<int> SaveChangesAsync();
}
