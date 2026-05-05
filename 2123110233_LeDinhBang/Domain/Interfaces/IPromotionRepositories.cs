using BookStore.Domain.Entities;

namespace BookStore.Domain.Interfaces;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdAsync(Guid id);
    Task<Voucher?> GetByCodeAsync(string code);
    Task<(IEnumerable<Voucher> Items, int Total)> GetPagedAsync(int page, int pageSize, bool? isActive);
    Task<int> GetUserUsageCountAsync(Guid voucherId, Guid userId);
    Task IncrementUsageAsync(Guid voucherId, Guid userId, Guid orderId, decimal discountApplied);
    Task DecrementUsageAsync(Guid voucherId, Guid userId);
    Task AddAsync(Voucher voucher);
    void Update(Voucher voucher);
    Task<int> SaveChangesAsync();
}

public interface IFlashSaleRepository
{
    Task<FlashSale?> GetByIdAsync(Guid id);
    Task<FlashSale?> GetActiveAsync();
    Task<(IEnumerable<FlashSale> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task AddAsync(FlashSale flashSale);
    void Update(FlashSale flashSale);
    Task<int> SaveChangesAsync();
}
