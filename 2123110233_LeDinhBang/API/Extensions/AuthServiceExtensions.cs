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
        // 1. Lấy chuỗi kết nối từ cấu hình (Nó sẽ tự tìm trong appsettings hoặc Environment Variables)
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                connectionString, // Không dán chết nữa, dùng biến này!
                sqlOptions => sqlOptions.MigrationsAssembly("2123110233_LeDinhBang")
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
