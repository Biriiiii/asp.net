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
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                // DÁN TRỰC TIẾP CHUỖI KẾT NỐI VÀO ĐÂY:
                "Server=.;Database=Book_Net8;User Id=sa;Password=123456;TrustServerCertificate=True;",
                sqlOptions => sqlOptions.MigrationsAssembly("2123110233_LeDinhBang")
            )
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
