using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Domain.Entities;

/// <summary>Voucher giảm giá — percent hoặc fixed, giới hạn theo user và tổng lượt</summary>
public class Voucher : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "percent";   // percent | fixed
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }         // Giới hạn tối đa khi %
    public decimal MinOrderValue { get; set; } = 0;
    public int TotalUsageLimit { get; set; } = 0;           // 0 = không giới hạn
    public int PerUserLimit { get; set; } = 1;
    public int UsedCount { get; set; } = 0;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VoucherUsage> Usages { get; set; } = new List<VoucherUsage>();
}

/// <summary>Audit trail mỗi lượt sử dụng voucher</summary>
public class VoucherUsage : BaseEntity
{
    public Guid VoucherId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    public Voucher Voucher { get; set; } = null!;
}

/// <summary>Flash Sale — giảm giá theo khung giờ, giới hạn số lượng mỗi sản phẩm</summary>
public class FlashSale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FlashSaleItem> Items { get; set; } = new List<FlashSaleItem>();

    [NotMapped] public bool IsOngoing =>
        IsActive && DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
}

/// <summary>Sản phẩm trong Flash Sale — giới hạn số lượng, tự hết khi SoldCount đủ</summary>
public class FlashSaleItem : BaseEntity
{
    public Guid FlashSaleId { get; set; }
    public Guid ProductId { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
    public int SoldCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public FlashSale FlashSale { get; set; } = null!;
    public Product? Product { get; set; }

    [NotMapped] public bool IsAvailable => IsActive && SoldCount < QuantityLimit;
    [NotMapped] public int Remaining    => QuantityLimit - SoldCount;
}
