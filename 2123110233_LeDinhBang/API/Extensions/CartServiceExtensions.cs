using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Repositories;

namespace BookStore.API.Extensions;

public static class CartServiceExtensions
{
    public static IServiceCollection AddCartModule(this IServiceCollection services)
    {
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartService,    CartService>();
        return services;
    }
}
