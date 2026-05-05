using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.API.Extensions;

public static class CloudinaryServiceExtensions
{
    public static IServiceCollection AddCloudinaryModule(this IServiceCollection services, IConfiguration config)
    {
        // Kiểm tra config có đủ không
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey    = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey)    ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình Cloudinary. Vui lòng thêm Cloudinary:CloudName, " +
                "Cloudinary:ApiKey, Cloudinary:ApiSecret vào appsettings.json hoặc Environment Variables.");
        }

        services.AddScoped<IImageUploadService, CloudinaryImageService>();
        return services;
    }
}
