using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Warehouse;

// ── Supplier Responses ────────────────────────────────────
public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    bool IsActive,
    DateTime CreatedAt
);

// ── PurchaseOrder Responses ───────────────────────────────
public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    string Status,
    SupplierDto Supplier,
    decimal TotalAmount,
    string? Note,
    Guid CreatedBy,
    Guid? ApprovedBy,
    DateTime? ApprovedAt,
    IEnumerable<PurchaseOrderItemDto> Items,
    DateTime CreatedAt
);

public record PurchaseOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductTitle,
    string? ProductIsbn,
    int QtyOrdered,
    int QtyReceived,
    int QtyPending,
    decimal UnitCost,
    decimal LineTotal,
    string? Note
);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext  => Page < TotalPages;
    public bool HasPrev  => Page > 1;
}

// ── Supplier Requests ─────────────────────────────────────
public class CreateSupplierRequest
{
    [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(200)] public string? ContactName { get; set; }
    [Phone][MaxLength(15)] public string? Phone { get; set; }
    [EmailAddress][MaxLength(200)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(20)] public string? TaxCode { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── PurchaseOrder Requests ────────────────────────────────
public class CreatePurchaseOrderRequest
{
    [Required] public Guid SupplierId { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    [Required][MinLength(1)] public List<PurchaseOrderItemRequest> Items { get; set; } = new();
}

public class PurchaseOrderItemRequest
{
    [Required] public Guid ProductId { get; set; }
    [Range(1, 999999)] public int QtyOrdered { get; set; }
    [Range(0.01, 999999999)] public decimal UnitCost { get; set; }
    [MaxLength(200)] public string? Note { get; set; }
}

public class ReceivePurchaseOrderRequest
{
    [Required][MinLength(1)] public List<ReceiveItemRequest> Items { get; set; } = new();
}

public class ReceiveItemRequest
{
    [Required] public Guid ItemId { get; set; }
    [Range(1, 999999)] public int QtyReceived { get; set; }
}
