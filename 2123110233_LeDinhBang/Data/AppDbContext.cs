using _2123110233_LeDinhBang.Models;
using Microsoft.EntityFrameworkCore;

namespace _2123110233_LeDinhBang.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAuthor> ProductAuthors { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Category tự trỏ (Danh mục cha - con)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Cấu hình khóa chính kép cho bảng trung gian ProductAuthor (Nhiều - Nhiều)
            modelBuilder.Entity<ProductAuthor>()
                .HasKey(pa => new { pa.ProductId, pa.AuthorId });

            modelBuilder.Entity<ProductAuthor>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.ProductAuthors)
                .HasForeignKey(pa => pa.ProductId);

            modelBuilder.Entity<ProductAuthor>()
                .HasOne(pa => pa.Author)
                .WithMany(a => a.ProductAuthors)
                .HasForeignKey(pa => pa.AuthorId);

            // 3. Cấu hình quan hệ 1 - 1 giữa Product và Inventory
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Inventory)
                .WithOne(i => i.Product)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa sách thì xóa luôn thông tin tồn kho
        }
    }
}

