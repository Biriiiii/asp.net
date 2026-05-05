using BookStore.Application.DTOs.Promotion;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Services;

// ── VoucherService ────────────────────────────────────────
public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _vouchers;
    public VoucherService(IVoucherRepository vouchers) => _vouchers = vouchers;

    public async Task<VoucherValidateDto> ValidateAsync(string code, Guid userId, decimal subtotal)
    {
        var v = await _vouchers.GetByCodeAsync(code);
        if (v == null) return Fail("Mã voucher không tồn tại.", subtotal);
        if (!v.IsActive || DateTime.UtcNow < v.StartDate || DateTime.UtcNow > v.EndDate)
            return Fail("Voucher đã hết hạn hoặc chưa kích hoạt.", subtotal);
        if (v.MinOrderValue > 0 && subtotal < v.MinOrderValue)
            return Fail($"Đơn hàng tối thiểu {v.MinOrderValue:N0}đ để dùng voucher này.", subtotal);
        if (v.TotalUsageLimit > 0 && v.UsedCount >= v.TotalUsageLimit)
            return Fail("Voucher đã hết lượt sử dụng.", subtotal);

        var userUsed = await _vouchers.GetUserUsageCountAsync(v.Id, userId);
        if (v.PerUserLimit > 0 && userUsed >= v.PerUserLimit)
            return Fail("Bạn đã dùng hết lượt voucher này.", subtotal);

        var discount = v.DiscountType == "percent"
            ? Math.Min(subtotal * v.DiscountValue / 100, v.MaxDiscountAmount ?? decimal.MaxValue)
            : Math.Min(v.DiscountValue, subtotal);

        return new VoucherValidateDto(true, null, discount, subtotal - discount);
    }

    public async Task<PagedResult<VoucherDto>> GetPagedAsync(int page, int pageSize, bool? isActive)
    {
        var (items, total) = await _vouchers.GetPagedAsync(page, pageSize, isActive);
        return new PagedResult<VoucherDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<VoucherDto> GetByIdAsync(Guid id)
    {
        var v = await _vouchers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Voucher Id={id} không tồn tại.");
        return Map(v);
    }

    public async Task<VoucherDto> CreateAsync(CreateVoucherRequest req)
    {
        if (await _vouchers.GetByCodeAsync(req.Code) != null)
            throw new InvalidOperationException($"Mã voucher '{req.Code}' đã tồn tại.");
        if (req.EndDate <= req.StartDate)
            throw new InvalidOperationException("EndDate phải sau StartDate.");

        var v = new Voucher
        {
            Code = req.Code.Trim().ToUpper(), Name = req.Name.Trim(),
            DiscountType = req.DiscountType, DiscountValue = req.DiscountValue,
            MaxDiscountAmount = req.MaxDiscountAmount, MinOrderValue = req.MinOrderValue,
            TotalUsageLimit = req.TotalUsageLimit, PerUserLimit = req.PerUserLimit,
            StartDate = req.StartDate, EndDate = req.EndDate, IsActive = req.IsActive
        };
        await _vouchers.AddAsync(v);
        await _vouchers.SaveChangesAsync();
        return Map(v);
    }

    public async Task<VoucherDto> UpdateAsync(Guid id, CreateVoucherRequest req)
    {
        var v = await _vouchers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Voucher Id={id} không tồn tại.");
        v.Name = req.Name.Trim(); v.DiscountType = req.DiscountType;
        v.DiscountValue = req.DiscountValue; v.MaxDiscountAmount = req.MaxDiscountAmount;
        v.MinOrderValue = req.MinOrderValue; v.TotalUsageLimit = req.TotalUsageLimit;
        v.PerUserLimit = req.PerUserLimit; v.StartDate = req.StartDate;
        v.EndDate = req.EndDate; v.IsActive = req.IsActive; v.UpdatedAt = DateTime.UtcNow;
        _vouchers.Update(v);
        await _vouchers.SaveChangesAsync();
        return Map(v);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var v = await _vouchers.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Voucher Id={id} không tồn tại.");
        v.IsActive = false; v.UpdatedAt = DateTime.UtcNow;
        _vouchers.Update(v);
        await _vouchers.SaveChangesAsync();
    }

    private static VoucherValidateDto Fail(string msg, decimal subtotal) =>
        new(false, msg, 0, subtotal);

    private static VoucherDto Map(Voucher v) =>
        new(v.Id, v.Code, v.Name, v.DiscountType, v.DiscountValue, v.MaxDiscountAmount,
            v.MinOrderValue, v.TotalUsageLimit, v.PerUserLimit, v.UsedCount,
            v.StartDate, v.EndDate, v.IsActive);
}

// ── FlashSaleService ──────────────────────────────────────
public class FlashSaleService : IFlashSaleService
{
    private readonly IFlashSaleRepository _flashSales;
    public FlashSaleService(IFlashSaleRepository flashSales) => _flashSales = flashSales;

    public async Task<FlashSaleDto?> GetActiveAsync()
    {
        var fs = await _flashSales.GetActiveAsync();
        return fs == null ? null : Map(fs);
    }

    public async Task<PagedResult<FlashSaleDto>> GetPagedAsync(int page, int pageSize)
    {
        var (items, total) = await _flashSales.GetPagedAsync(page, pageSize);
        return new PagedResult<FlashSaleDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<FlashSaleDto> GetByIdAsync(Guid id)
    {
        var fs = await _flashSales.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flash sale Id={id} không tồn tại.");
        return Map(fs);
    }

    public async Task<FlashSaleDto> CreateAsync(CreateFlashSaleRequest req)
    {
        if (req.EndTime <= req.StartTime)
            throw new InvalidOperationException("EndTime phải sau StartTime.");

        var fs = new FlashSale
        {
            Name = req.Name.Trim(), Description = req.Description,
            StartTime = req.StartTime, EndTime = req.EndTime, IsActive = req.IsActive
        };
        foreach (var item in req.Items)
            fs.Items.Add(new FlashSaleItem
            {
                FlashSaleId = fs.Id, ProductId = item.ProductId,
                SalePrice = item.SalePrice, QuantityLimit = item.QuantityLimit
            });

        await _flashSales.AddAsync(fs);
        await _flashSales.SaveChangesAsync();
        return Map(fs);
    }

    public async Task<FlashSaleDto> UpdateAsync(Guid id, CreateFlashSaleRequest req)
    {
        var fs = await _flashSales.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flash sale Id={id} không tồn tại.");
        fs.Name = req.Name.Trim(); fs.Description = req.Description;
        fs.StartTime = req.StartTime; fs.EndTime = req.EndTime;
        fs.IsActive = req.IsActive; fs.UpdatedAt = DateTime.UtcNow;
        _flashSales.Update(fs);
        await _flashSales.SaveChangesAsync();
        return Map(fs);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var fs = await _flashSales.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Flash sale Id={id} không tồn tại.");
        fs.IsActive = false; fs.UpdatedAt = DateTime.UtcNow;
        _flashSales.Update(fs);
        await _flashSales.SaveChangesAsync();
    }

    private static FlashSaleDto Map(FlashSale fs) =>
        new(fs.Id, fs.Name, fs.Description, fs.StartTime, fs.EndTime, fs.IsActive, fs.IsOngoing,
            fs.Items.Select(i => new FlashSaleItemDto(
                i.Id, i.ProductId, i.Product?.Title ?? "",
                i.Product?.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
                i.Product?.OriginalPrice ?? 0, i.SalePrice,
                i.Product?.OriginalPrice > 0
                    ? (int)Math.Round((i.Product.OriginalPrice - i.SalePrice) / i.Product.OriginalPrice * 100) : 0,
                i.QuantityLimit, i.SoldCount, i.Remaining, i.IsAvailable)));
}
