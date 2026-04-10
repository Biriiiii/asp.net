namespace BookStore.Domain.Entities;

public class Inventory : BaseEntity
{
    public Guid ProductId { get; set; }
    public int QtyAvailable { get; set; } = 0;
    public int QtyReserved { get; set; } = 0;
    public int QtySold { get; set; } = 0;
    public int MinThreshold { get; set; } = 5;
    public string? WarehouseLocation { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed (không lưu DB)
    public int QtyActual => QtyAvailable - QtyReserved;
    public bool IsLowStock => QtyActual <= MinThreshold;
    public bool IsOutOfStock => QtyActual <= 0;

    // Navigation
    public Product Product { get; set; } = null!;
}
