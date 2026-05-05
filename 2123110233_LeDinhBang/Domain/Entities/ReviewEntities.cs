// ═══════════════════════════════════════════════════════════
// FILE: Review/Domain/Entities/ReviewEntities.cs
// ═══════════════════════════════════════════════════════════
namespace BookStore.Domain.Entities;

/// <summary>Đánh giá sản phẩm — chỉ cho phép sau khi đơn DELIVERED, xác minh qua OrderId</summary>
public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }       // Xác minh đã mua hàng
    public int Rating { get; set; }         // 1-5 sao
    public string? Title { get; set; }
    public string? Content { get; set; }
    public bool IsVisible { get; set; } = true;
    public int HelpfulCount { get; set; }  = 0;

    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}

/// <summary>Ảnh đính kèm review — tối đa 5 ảnh</summary>
public class ReviewImage : BaseEntity
{
    public Guid ReviewId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;

    public Review Review { get; set; } = null!;
}
