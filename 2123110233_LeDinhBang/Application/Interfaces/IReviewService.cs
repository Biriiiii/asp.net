// ═══════════════════════════════════════════════════════════
// FILE: Review/Application/Interfaces/IReviewService.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Application.DTOs.Review;

namespace BookStore.Application.Interfaces;

public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetByProductAsync(Guid productId, int page, int pageSize, string sort);
    Task<ReviewSummaryDto> GetSummaryAsync(Guid productId);
    Task<ReviewDto> CreateAsync(Guid userId, CreateReviewRequest request);
    Task<ReviewDto> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewRequest request);
    Task DeleteAsync(Guid userId, Guid reviewId, bool isAdmin = false);
    Task ToggleVisibilityAsync(Guid reviewId);   // Admin: ẩn/hiện review
    Task MarkHelpfulAsync(Guid reviewId);        // Customer: vote helpful
}
