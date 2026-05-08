using BookStore.Application.Interfaces;
using BookStore.Application.Services;

namespace BookStore.API.Extensions;

public static class DashboardServiceExtensions
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
