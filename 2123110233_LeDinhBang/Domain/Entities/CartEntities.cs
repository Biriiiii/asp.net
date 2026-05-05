using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Domain.Entities;

/// <summary>Giỏ hàng — hỗ trợ cả Guest (SessionId) và Customer (UserId)</summary>
public class Cart : BaseEntity
{
    public Guid? UserId { get; set; }           // null = guest
    public string? SessionId { get; set; }      // guest session token
    public DateTime? ExpiresAt { get; set; }    // guest cart hết hạn sau 7 ngày

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    [NotMapped] public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
    [NotMapped] public int TotalItems   => Items.Sum(i => i.Quantity);
}

/// <summary>Item trong giỏ hàng — lưu snapshot giá lúc thêm vào</summary>
public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }      // Snapshot giá lúc thêm

    public Cart Cart { get; set; } = null!;
    public Product? Product { get; set; }

    [NotMapped] public decimal LineTotal => UnitPrice * Quantity;
}
