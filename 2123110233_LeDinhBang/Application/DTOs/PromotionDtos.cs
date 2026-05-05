using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Promotion;

// ── Voucher Responses ─────────────────────────────────────
public record VoucherDto(
    Guid Id, string Code, string Name,
    string DiscountType, decimal DiscountValue, decimal? MaxDiscountAmount,
    decimal MinOrderValue, int TotalUsageLimit, int PerUserLimit, int UsedCount,
    DateTime StartDate, DateTime EndDate, bool IsActive
);

public record VoucherValidateDto(
    bool IsValid, string? ErrorMessage,
    decimal DiscountAmount, decimal FinalTotal
);

// ── FlashSale Responses ───────────────────────────────────
public record FlashSaleDto(
    Guid Id, string Name, string? Description,
    DateTime StartTime, DateTime EndTime,
    bool IsActive, bool IsOngoing,
    IEnumerable<FlashSaleItemDto> Items
);

public record FlashSaleItemDto(
    Guid Id, Guid ProductId, string Title, string? CoverUrl,
    decimal OriginalPrice, decimal SalePrice, int DiscountPercent,
    int QuantityLimit, int SoldCount, int Remaining, bool IsAvailable
);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext  => Page < TotalPages;
    public bool HasPrev  => Page > 1;
}

// ── Voucher Requests ──────────────────────────────────────
public class CreateVoucherRequest
{
    [Required][MaxLength(50)]  public string Code { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public string DiscountType { get; set; } = "percent";   // percent | fixed
    [Range(0.01, 999999999)] public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    [Range(0, 999999999)] public decimal MinOrderValue { get; set; } = 0;
    public int TotalUsageLimit { get; set; } = 0;
    public int PerUserLimit { get; set; } = 1;
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ValidateVoucherRequest
{
    [Required] public string Code { get; set; } = string.Empty;
    [Range(0, 999999999)] public decimal Subtotal { get; set; }
}

// ── FlashSale Requests ────────────────────────────────────
public class CreateFlashSaleRequest
{
    [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required] public DateTime StartTime { get; set; }
    [Required] public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    [Required] public List<FlashSaleItemRequest> Items { get; set; } = new();
}

public class FlashSaleItemRequest
{
    [Required] public Guid ProductId { get; set; }
    [Range(0.01, 999999999)] public decimal SalePrice { get; set; }
    [Range(1, 999999)] public int QuantityLimit { get; set; }
}
