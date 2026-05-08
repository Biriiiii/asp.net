using BookStore.Application.DTOs.Review;
using BookStore.Application.DTOs.Promotion;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviews;
    public ReviewService(IReviewRepository reviews) => _reviews = reviews;

    public async Task<PagedResult<ReviewDto>> GetByProductAsync(
        Guid productId, int page, int pageSize, string sort)
    {
        var (items, total) = await _reviews.GetByProductAsync(productId, page, pageSize, sort);
        return new PagedResult<ReviewDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<ReviewSummaryDto> GetSummaryAsync(Guid productId)
    {
        var (items, total) = await _reviews.GetByProductAsync(productId, 1, int.MaxValue, "newest");
        var list = items.ToList();
        var avg  = total > 0 ? list.Average(r => r.Rating) : 0;
        var dist = Enumerable.Range(1, 5).ToDictionary(s => s, s => list.Count(r => r.Rating == s));
        return new ReviewSummaryDto(Math.Round(avg, 1), total, dist);
    }

    public async Task<ReviewDto> CreateAsync(Guid userId, CreateReviewRequest req)
    {
        if (!await _reviews.HasPurchasedAndDeliveredAsync(userId, req.ProductId))
            throw new InvalidOperationException("Bạn chỉ có thể đánh giá sản phẩm đã mua và đã nhận hàng.");

        if (await _reviews.GetByUserAndProductAsync(userId, req.ProductId) != null)
            throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");

        var images = req.Images?.ToList() ?? new List<string>();
        if (images.Count > 5)
            throw new InvalidOperationException("Tối đa 5 ảnh mỗi đánh giá.");

        var review = new Review
        {
            ProductId = req.ProductId, UserId = userId, OrderId = req.OrderId,
            Rating = req.Rating, Title = req.Title?.Trim(), Content = req.Content?.Trim()
        };

        for (int i = 0; i < images.Count; i++)
            review.Images.Add(new ReviewImage
            {
                ReviewId = review.Id, ImageUrl = images[i], DisplayOrder = i
            });

        await _reviews.AddAsync(review);
        await _reviews.SaveChangesAsync();
        return Map(review);
    }

    public async Task<ReviewDto> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewRequest req)
    {
        var review = await _reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Đánh giá không tồn tại.");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa đánh giá này.");

        if ((DateTime.UtcNow - review.CreatedAt).TotalDays > 7)
            throw new InvalidOperationException("Chỉ chỉnh sửa đánh giá trong vòng 7 ngày.");

        review.Rating = req.Rating; review.Title = req.Title?.Trim();
        review.Content = req.Content?.Trim(); review.UpdatedAt = DateTime.UtcNow;
        _reviews.Update(review);
        await _reviews.SaveChangesAsync();
        return Map(review);
    }

    public async Task DeleteAsync(Guid userId, Guid reviewId, bool isAdmin = false)
    {
        var review = await _reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Đánh giá không tồn tại.");

        if (!isAdmin && review.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa đánh giá này.");

        _reviews.Delete(review);
        await _reviews.SaveChangesAsync();
    }

    public async Task ToggleVisibilityAsync(Guid reviewId)
    {
        var review = await _reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Đánh giá không tồn tại.");
        review.IsVisible = !review.IsVisible; review.UpdatedAt = DateTime.UtcNow;
        _reviews.Update(review);
        await _reviews.SaveChangesAsync();
    }

    public async Task MarkHelpfulAsync(Guid reviewId)
    {
        var review = await _reviews.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Đánh giá không tồn tại.");
        review.HelpfulCount++;
        _reviews.Update(review);
        await _reviews.SaveChangesAsync();
    }

    private static ReviewDto Map(Review r) =>
        new(r.Id, r.ProductId, r.UserId,
            "", null,   // UserFullName & AvatarUrl: join từ Users nếu cần
            r.Rating, r.Title, r.Content, r.HelpfulCount, r.IsVisible,
            r.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl),
            r.CreatedAt, r.UpdatedAt);
}
