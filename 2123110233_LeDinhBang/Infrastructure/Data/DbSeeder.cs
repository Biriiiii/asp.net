using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AuthDbContext authDb, AppDbContext appDb)
    {
        // 1. Tự động chạy Migration
        await authDb.Database.MigrateAsync();
        await appDb.Database.MigrateAsync();

        // 2. Seed User Admin
        if (!await authDb.Users.AnyAsync())
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@bookstore.com",
                FullName = "Hệ Thống Admin",
                PasswordHash = ComputeHash("Admin123@"),
                Gender = Gender.Male,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.UserRoles.Add(new UserRole_ { Role = UserRole.Admin });
            authDb.Users.Add(admin);
            await authDb.SaveChangesAsync();
            Console.WriteLine("--> Đã nạp tài khoản Admin mẫu thành công.");
        }

        // 3. Seed Danh mục & Sản phẩm
        if (!await appDb.Categories.AnyAsync())
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Lập trình",
                Slug = "lap-trinh",
                IsActive = true
            };
            appDb.Categories.Add(category);

            // Thêm sản phẩm mẫu
            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Title = "Lập trình .NET Core API",
                Slug = "lap-trinh-dotnet-core-api",
                Isbn = "123456789",
                OriginalPrice = 150000,
                SalePrice = 100000,
                IsActive = true,
                WeightGram = 500
            };
            product.Inventory = new Inventory { ProductId = product.Id, QtyAvailable = 100 };
            appDb.Products.Add(product);
            
            await appDb.SaveChangesAsync();
            Console.WriteLine("--> Đã nạp danh mục và sản phẩm mẫu thành công.");
        }

        // 4. Seed Voucher mẫu
        if (!await appDb.Vouchers.AnyAsync())
        {
            appDb.Vouchers.AddRange(
                new Voucher
                {
                    Id = Guid.NewGuid(),
                    Code = "HELLO2024",
                    Name = "Giảm 10% cho khách mới",
                    DiscountType = "percent",
                    DiscountValue = 10,
                    MaxDiscountAmount = 50000,
                    MinOrderValue = 100000,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true
                },
                new Voucher 
                {
                    Id = Guid.NewGuid(),
                    Code = "GIAM20K",
                    Name = "Giảm giá 20K",
                    DiscountType = "fixed",
                    DiscountValue = 20000,
                    MinOrderValue = 0,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true
                }
            );
            await appDb.SaveChangesAsync();
            Console.WriteLine("--> Đã nạp các mã giảm giá mẫu thành công.");
        }
    }

    private static string ComputeHash(string password)
    {
        const string salt = "BookStore_Salt_2024";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(bytes);
    }
}