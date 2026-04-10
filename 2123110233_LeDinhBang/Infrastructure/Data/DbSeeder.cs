using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Infrastructure.Data;

public static class DbSeeder
{
    // Các ID cố định để tạo mối liên kết chính xác
    private static readonly Guid CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid PublisherId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid AuthorId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("50000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext appDb, AuthDbContext authDb)
    {
        // 1. Seed dữ liệu AUTH (Người dùng & Quyền)
        if (!await authDb.Users.AnyAsync())
        {
            var adminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var admin = new User
            {
                Id = adminId,
                Email = "admin@bookstore.com",
                PasswordHash = ComputeHash("123456"),
                FullName = "Quản Trị Viên",
                Phone = "0987654321",
                IsActive = true,
                EmailVerified = true,
                PhoneVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Gán quyền Admin (Role = 4)
            admin.UserRoles.Add(new UserRole_
            {
                UserId = adminId,
                Role = UserRole.Admin,
                AssignedAt = DateTime.UtcNow
            });

            await authDb.Users.AddAsync(admin);
            await authDb.SaveChangesAsync();
        }

        // 2. Seed dữ liệu APP (Danh mục, NXB, Tác giả, Sản phẩm)

        // --- 2.1 Seed Danh mục ---
        if (!await appDb.Categories.AnyAsync())
        {
            await appDb.Categories.AddAsync(new Category
            {
                Id = CategoryId,
                Name = "Lập Trình",
                Slug = "lap-trinh",
                Description = "Sách công nghệ thông tin",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // --- 2.2 Seed Nhà xuất bản ---
        if (!await appDb.Publishers.AnyAsync())
        {
            await appDb.Publishers.AddAsync(new Publisher
            {
                Id = PublisherId,
                Name = "NXB Trẻ",
                Country = "Việt Nam",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // --- 2.3 Seed Tác giả ---
        if (!await appDb.Authors.AnyAsync())
        {
            await appDb.Authors.AddAsync(new Author
            {
                Id = AuthorId,
                Name = "Robert C. Martin",
                Nationality = "Mỹ",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await appDb.SaveChangesAsync(); // Lưu để lấy ID cho bước sau

        // --- 2.4 Seed Sản phẩm (Sách) ---
        if (!await appDb.Products.AnyAsync())
        {
            var product = new Product
            {
                Id = ProductId,
                CategoryId = CategoryId,
                PublisherId = PublisherId,
                Title = "Clean Code",
                Slug = "clean-code",
                PageCount = 464,
                WeightGram = 800,
                Language = "vi",
                OriginalPrice = 500000,
                SalePrice = 450000,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Nối tác giả vào sách
            product.ProductAuthors.Add(new ProductAuthor
            {
                ProductId = ProductId,
                AuthorId = AuthorId,
                Role = "Tác giả chính"
            });

            // Tạo tồn kho ban đầu
            product.Inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = ProductId,
                QtyAvailable = 100,
                MinThreshold = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await appDb.Products.AddAsync(product);
            await appDb.SaveChangesAsync();
        }
    }

    private static string ComputeHash(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;
        using var sha = SHA256.Create();
        // QUAN TRỌNG: Salt phải khớp 100% với AuthService.cs
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "BookStore_Salt_2024"));
        return Convert.ToBase64String(bytes);
    }
}