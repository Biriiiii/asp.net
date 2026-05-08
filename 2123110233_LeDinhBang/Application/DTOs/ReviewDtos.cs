using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Review;

public record ReviewDto(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    string UserName,
    string? UserAvatar,
    int Rating,
    string? Title,
    string? Content,
    int HelpfulCount,
    bool IsVisible,
    IEnumerable<string> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateReviewRequest(
    [Required] Guid ProductId,
    [Required] Guid OrderId,
    [Range(1, 5)] int Rating,
    [MaxLength(200)] string? Title,
    [MaxLength(2000)] string? Content,
    IEnumerable<string>? Images
);

public record UpdateReviewRequest(
    [Range(1, 5)] int Rating,
    [MaxLength(200)] string? Title,
    [MaxLength(2000)] string? Content,
    IEnumerable<string>? Images
);

public record ReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> RatingDistribution
);
