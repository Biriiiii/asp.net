using BookStore.Domain.Entities;

namespace BookStore.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Supplier> Items, int Total)> GetPagedAsync(string? keyword, int page, int pageSize);
    Task AddAsync(Supplier supplier);
    void Update(Supplier supplier);
    Task<int> SaveChangesAsync();
}

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id);
    Task<PurchaseOrder?> GetDetailAsync(Guid id);   // include Supplier + Items + Product
    Task<(IEnumerable<PurchaseOrder> Items, int Total)> GetPagedAsync(string? status, int page, int pageSize);
    Task AddAsync(PurchaseOrder po);
    void Update(PurchaseOrder po);
    Task<int> SaveChangesAsync();
}
