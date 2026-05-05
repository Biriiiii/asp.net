// ═══════════════════════════════════════════════════════════
// FILE: Warehouse/Domain/Entities/WarehouseEntities.cs
// ═══════════════════════════════════════════════════════════
namespace BookStore.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>Nhà cung cấp sách</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

/// <summary>Phiếu nhập hàng — có trạng thái phê duyệt (ApprovedBy)</summary>
public class PurchaseOrder : BaseEntity
{
    public Guid SupplierId { get; set; }
    public string PoNumber { get; set; } = string.Empty;   // PO20240401001
    public string Status { get; set; } = "Draft";          // Draft | Approved | Received | Cancelled
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

/// <summary>Item trong phiếu nhập — theo dõi SL đặt và SL thực nhận</summary>
public class PurchaseOrderItem : BaseEntity
{
    public Guid PoId { get; set; }
    public Guid ProductId { get; set; }
    public int QtyOrdered { get; set; }
    public int QtyReceived { get; set; } = 0;
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product? Product { get; set; }

    [NotMapped] public decimal LineTotal => UnitCost * QtyOrdered;
    [NotMapped] public int QtyPending   => QtyOrdered - QtyReceived;
}
