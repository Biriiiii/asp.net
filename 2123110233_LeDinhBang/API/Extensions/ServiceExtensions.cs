using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        // LẤY CHUỖI KẾT NỐI ONLINE
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString) // Sử dụng biến connectionString
        );
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository,   ProductRepository>();
        services.AddScoped<ICategoryRepository,  CategoryRepository>();
        services.AddScoped<IAuthorRepository,    AuthorRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        return services;
    }

    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService,   ProductService>();
        services.AddScoped<ICategoryService,  CategoryService>();
        services.AddScoped<IAuthorService,    AuthorService>();
        services.AddScoped<IPublisherService, PublisherService>();
        return services;
    }
}
