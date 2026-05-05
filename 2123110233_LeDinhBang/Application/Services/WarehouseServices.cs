using BookStore.Application.DTOs.Warehouse;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Services;

// ── SupplierService ───────────────────────────────────────
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _suppliers;
    public SupplierService(ISupplierRepository suppliers) => _suppliers = suppliers;

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(string? keyword, int page, int pageSize)
    {
        var (items, total) = await _suppliers.GetPagedAsync(keyword, page, pageSize);
        return new PagedResult<SupplierDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<SupplierDto> GetByIdAsync(Guid id)
    {
        var s = await _suppliers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà cung cấp Id={id} không tồn tại.");
        return Map(s);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest req)
    {
        var supplier = new Supplier
        {
            Name        = req.Name.Trim(),
            ContactName = req.ContactName?.Trim(),
            Phone       = req.Phone?.Trim(),
            Email       = req.Email?.Trim(),
            Address     = req.Address?.Trim(),
            TaxCode     = req.TaxCode?.Trim(),
            IsActive    = req.IsActive
        };
        await _suppliers.AddAsync(supplier);
        await _suppliers.SaveChangesAsync();
        return Map(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, CreateSupplierRequest req)
    {
        var supplier = await _suppliers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà cung cấp Id={id} không tồn tại.");

        supplier.Name        = req.Name.Trim();
        supplier.ContactName = req.ContactName?.Trim();
        supplier.Phone       = req.Phone?.Trim();
        supplier.Email       = req.Email?.Trim();
        supplier.Address     = req.Address?.Trim();
        supplier.TaxCode     = req.TaxCode?.Trim();
        supplier.IsActive    = req.IsActive;
        supplier.UpdatedAt   = DateTime.UtcNow;

        _suppliers.Update(supplier);
        await _suppliers.SaveChangesAsync();
        return Map(supplier);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var supplier = await _suppliers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Nhà cung cấp Id={id} không tồn tại.");
        supplier.IsActive  = false;
        supplier.UpdatedAt = DateTime.UtcNow;
        _suppliers.Update(supplier);
        await _suppliers.SaveChangesAsync();
    }

    private static SupplierDto Map(Supplier s) =>
        new(s.Id, s.Name, s.ContactName, s.Phone,
            s.Email, s.Address, s.TaxCode, s.IsActive, s.CreatedAt);
}

// ── PurchaseOrderService ──────────────────────────────────
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _pos;
    private readonly ISupplierRepository      _suppliers;
    private readonly IProductRepository       _products;
    private readonly IInventoryRepository     _inventories;

    public PurchaseOrderService(
        IPurchaseOrderRepository pos,
        ISupplierRepository suppliers,
        IProductRepository products,
        IInventoryRepository inventories)
    {
        _pos         = pos;
        _suppliers   = suppliers;
        _products    = products;
        _inventories = inventories;
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(string? status, int page, int pageSize)
    {
        var (items, total) = await _pos.GetPagedAsync(status, page, pageSize);
        return new PagedResult<PurchaseOrderDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(Guid id)
    {
        var po = await _pos.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Phiếu nhập Id={id} không tồn tại.");
        return Map(po);
    }

    public async Task<PurchaseOrderDto> CreateAsync(Guid createdBy, CreatePurchaseOrderRequest req)
    {
        _ = await _suppliers.GetByIdAsync(req.SupplierId)
            ?? throw new KeyNotFoundException("Nhà cung cấp không tồn tại.");

        var po = new PurchaseOrder
        {
            SupplierId = req.SupplierId,
            PoNumber   = $"PO{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status     = "Draft",
            Note       = req.Note,
            CreatedBy  = createdBy
        };

        decimal total = 0;
        foreach (var item in req.Items)
        {
            _ = await _products.GetByIdAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"Sản phẩm Id={item.ProductId} không tồn tại.");

            po.Items.Add(new PurchaseOrderItem
            {
                PoId       = po.Id,
                ProductId  = item.ProductId,
                QtyOrdered = item.QtyOrdered,
                UnitCost   = item.UnitCost,
                Note       = item.Note
            });
            total += item.QtyOrdered * item.UnitCost;
        }
        po.TotalAmount = total;

        await _pos.AddAsync(po);
        await _pos.SaveChangesAsync();
        return await GetByIdAsync(po.Id);
    }

    public async Task<PurchaseOrderDto> ApproveAsync(Guid id, Guid approvedBy)
    {
        var po = await _pos.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Phiếu nhập Id={id} không tồn tại.");

        if (po.Status != "Draft")
            throw new InvalidOperationException("Chỉ phê duyệt phiếu ở trạng thái Draft.");

        po.Status     = "Approved";
        po.ApprovedBy = approvedBy;
        po.ApprovedAt = DateTime.UtcNow;
        po.UpdatedAt  = DateTime.UtcNow;

        _pos.Update(po);
        await _pos.SaveChangesAsync();
        return Map(po);
    }

    public async Task<PurchaseOrderDto> ReceiveAsync(Guid id, ReceivePurchaseOrderRequest req)
    {
        var po = await _pos.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Phiếu nhập Id={id} không tồn tại.");

        if (po.Status != "Approved")
            throw new InvalidOperationException("Chỉ nhận hàng khi phiếu đã được phê duyệt.");

        foreach (var receiveItem in req.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == receiveItem.ItemId)
                ?? throw new KeyNotFoundException($"Item Id={receiveItem.ItemId} không thuộc phiếu này.");

            if (receiveItem.QtyReceived > poItem.QtyPending)
                throw new InvalidOperationException(
                    $"Số lượng nhận ({receiveItem.QtyReceived}) vượt quá số lượng còn lại ({poItem.QtyPending}).");

            poItem.QtyReceived += receiveItem.QtyReceived;

            // Cộng trực tiếp vào tồn kho
            var inv = await _inventories.GetByProductIdAsync(poItem.ProductId);
            if (inv != null)
            {
                inv.QtyAvailable += receiveItem.QtyReceived;
                inv.UpdatedAt     = DateTime.UtcNow;
            }
        }

        // Nếu tất cả items đã nhận đủ → đóng phiếu
        if (po.Items.All(i => i.QtyReceived >= i.QtyOrdered))
            po.Status = "Received";

        po.UpdatedAt = DateTime.UtcNow;
        _pos.Update(po);
        await _pos.SaveChangesAsync();
        return Map(po);
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id)
    {
        var po = await _pos.GetDetailAsync(id)
            ?? throw new KeyNotFoundException($"Phiếu nhập Id={id} không tồn tại.");

        if (po.Status == "Received")
            throw new InvalidOperationException("Không thể hủy phiếu đã nhận hàng.");

        po.Status    = "Cancelled";
        po.UpdatedAt = DateTime.UtcNow;
        _pos.Update(po);
        await _pos.SaveChangesAsync();
        return Map(po);
    }

    // ── Mapper ─────────────────────────────────────────────
    private static PurchaseOrderDto Map(PurchaseOrder po) =>
        new(po.Id, po.PoNumber, po.Status,
            new SupplierDto(po.Supplier.Id, po.Supplier.Name, po.Supplier.ContactName,
                po.Supplier.Phone, po.Supplier.Email, po.Supplier.Address,
                po.Supplier.TaxCode, po.Supplier.IsActive, po.Supplier.CreatedAt),
            po.TotalAmount, po.Note,
            po.CreatedBy, po.ApprovedBy, po.ApprovedAt,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id, i.ProductId,
                i.Product?.Title ?? "",
                i.Product?.Isbn,
                i.QtyOrdered, i.QtyReceived, i.QtyPending,
                i.UnitCost, i.LineTotal, i.Note)),
            po.CreatedAt);
}
