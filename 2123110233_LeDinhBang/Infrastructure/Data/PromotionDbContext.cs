// ═══════════════════════════════════════════════════════════
// FILE: Promotion/Infrastructure/Data/PromotionDbContext.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<Voucher>       Vouchers       => Set<Voucher>();
    public DbSet<VoucherUsage>  VoucherUsages  => Set<VoucherUsage>();
    public DbSet<FlashSale>     FlashSales     => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

    private void ConfigurePromotion(ModelBuilder mb)
    {
        mb.Entity<Voucher>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DiscountType).HasMaxLength(10);
            e.Property(x => x.DiscountValue).HasColumnType("decimal(15,2)");
            e.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(15,2)");
            e.Property(x => x.MinOrderValue).HasColumnType("decimal(15,2)");
        });

        mb.Entity<VoucherUsage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.VoucherId, x.UserId });
            e.Property(x => x.DiscountApplied).HasColumnType("decimal(15,2)");
            e.HasOne(x => x.Voucher).WithMany(x => x.Usages)
             .HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FlashSale>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => new { x.StartTime, x.EndTime, x.IsActive });
            e.Ignore(x => x.IsOngoing);
        });

        mb.Entity<FlashSaleItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FlashSaleId, x.ProductId }).IsUnique();
            e.Property(x => x.SalePrice).HasColumnType("decimal(15,2)");
            e.Ignore(x => x.IsAvailable);
            e.Ignore(x => x.Remaining);
            e.HasOne(x => x.FlashSale).WithMany(x => x.Items)
             .HasForeignKey(x => x.FlashSaleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
