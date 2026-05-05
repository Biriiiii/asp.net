using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Repositories;

namespace BookStore.API.Extensions;

public static class PromotionServiceExtensions
{
    public static IServiceCollection AddPromotionModule(this IServiceCollection services)
    {
        services.AddScoped<IVoucherRepository,   VoucherRepository>();
        services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
        services.AddScoped<IVoucherService,      VoucherService>();
        services.AddScoped<IFlashSaleService,    FlashSaleService>();
        return services;
    }
}
