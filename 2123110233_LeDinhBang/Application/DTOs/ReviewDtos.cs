using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Review;

public record ReviewDto(
    Guid Id, Guid ProductId, Guid UserId,
    string UserFullName, string? UserAvatarUrl,
    int Rating, string? Title, string? Content,
    int HelpfulCount, bool IsVisible,
    IEnumerable<string> ImageUrls,
    DateTime CreatedAt, DateTime UpdatedAt
);

public record ReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> StarDistribution   // { 5: 30, 4: 10, 3: 5, 2: 2, 1: 1 }
);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext  => Page < TotalPages;
    public bool HasPrev  => Page > 1;
}

public class CreateReviewRequest
{
    [Required] public Guid ProductId { get; set; }
    [Required] public Guid OrderId { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [MaxLength(200)] public string? Title { get; set; }
    [MaxLength(2000)] public string? Content { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateReviewRequest
{
    [Range(1, 5)] public int Rating { get; set; }
    [MaxLength(200)] public string? Title { get; set; }
    [MaxLength(2000)] public string? Content { get; set; }
}
