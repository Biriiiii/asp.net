using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BookStore.API.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly("BookStore.Infrastructure")
            )
        );
        return services;
    }

    public static IServiceCollection AddAuthRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository,          UserRepository>();
        services.AddScoped<IUserSessionRepository,   UserSessionRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IOtpRepository,           OtpRepository>();
        services.AddScoped<IAddressRepository,       AddressRepository>();
        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService,      TokenService>();
        services.AddScoped<IAuthService,       AuthService>();
        services.AddScoped<IUserService,       UserService>();
        services.AddScoped<IAdminUserService,  AdminUserService>();
        return services;
    }
}
