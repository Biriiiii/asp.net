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
        // 1. Tự động chạy Migration cho cả 2 Context
        await authDb.Database.MigrateAsync();
        await appDb.Database.MigrateAsync();

        // 2. Seed dữ liệu cho AuthDbContext (User, Roles)
        if (!await authDb.Users.AnyAsync())
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@bookstore.com",
                FullName = "Hệ Thống Admin",
                PasswordHash = ComputeHash("Admin123@"), // Mật khẩu mẫu
                Gender = Gender.Male,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            // Gán quyền Admin
            admin.UserRoles.Add(new UserRole_ { Role = UserRole.Admin });

            authDb.Users.Add(admin);
            await authDb.SaveChangesAsync();
            Console.WriteLine("--> Đã nạp tài khoản Admin mẫu thành công.");
        }

        // 3. Seed dữ liệu cho AppDbContext (Category, Product - ví dụ)
        if (!await appDb.Categories.AnyAsync())
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Văn học",
                Slug = "van-hoc",
                IsActive = true
            };
            appDb.Categories.Add(category);
            await appDb.SaveChangesAsync();
            Console.WriteLine("--> Đã nạp danh mục mẫu thành công.");
        }
    }

    // Hàm băm mật khẩu (phải giống hệt hàm trong AuthService)
    private static string ComputeHash(string password)
    {
        const string salt = "BookStore_Salt_2024";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(bytes);
    }
}