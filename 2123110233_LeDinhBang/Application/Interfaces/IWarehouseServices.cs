using BookStore.Application.DTOs.Warehouse;

namespace BookStore.Application.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(string? keyword, int page, int pageSize);
    Task<SupplierDto> GetByIdAsync(Guid id);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request);
    Task<SupplierDto> UpdateAsync(Guid id, CreateSupplierRequest request);
    Task DeactivateAsync(Guid id);
}

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(string? status, int page, int pageSize);
    Task<PurchaseOrderDto> GetByIdAsync(Guid id);
    Task<PurchaseOrderDto> CreateAsync(Guid createdBy, CreatePurchaseOrderRequest request);
    Task<PurchaseOrderDto> ApproveAsync(Guid id, Guid approvedBy);
    Task<PurchaseOrderDto> ReceiveAsync(Guid id, ReceivePurchaseOrderRequest request);
    Task<PurchaseOrderDto> CancelAsync(Guid id);
}
