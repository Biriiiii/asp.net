using BookStore.Application.DTOs.Promotion;

namespace BookStore.Application.Interfaces;

public interface IVoucherService
{
    Task<VoucherValidateDto> ValidateAsync(string code, Guid userId, decimal subtotal);
    Task<PagedResult<VoucherDto>> GetPagedAsync(int page, int pageSize, bool? isActive);
    Task<VoucherDto> GetByIdAsync(Guid id);
    Task<VoucherDto> CreateAsync(CreateVoucherRequest request);
    Task<VoucherDto> UpdateAsync(Guid id, CreateVoucherRequest request);
    Task DeactivateAsync(Guid id);
}

public interface IFlashSaleService
{
    Task<FlashSaleDto?> GetActiveAsync();
    Task<PagedResult<FlashSaleDto>> GetPagedAsync(int page, int pageSize);
    Task<FlashSaleDto> GetByIdAsync(Guid id);
    Task<FlashSaleDto> CreateAsync(CreateFlashSaleRequest request);
    Task<FlashSaleDto> UpdateAsync(Guid id, CreateFlashSaleRequest request);
    Task DeactivateAsync(Guid id);
}
