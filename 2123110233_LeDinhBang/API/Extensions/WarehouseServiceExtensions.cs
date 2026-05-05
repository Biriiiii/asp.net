using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Repositories;

namespace BookStore.API.Extensions;

public static class WarehouseServiceExtensions
{
    public static IServiceCollection AddWarehouseModule(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ISupplierRepository,       SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository,  PurchaseOrderRepository>();

        // Services
        services.AddScoped<ISupplierService,          SupplierService>();
        services.AddScoped<IPurchaseOrderService,     PurchaseOrderService>();

        return services;
    }
}
