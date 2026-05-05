// ═══════════════════════════════════════════════════════════
// FILE: Review/Infrastructure/Data/ReviewDbContext.cs
// ═══════════════════════════════════════════════════════════
using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<Review>      Reviews      => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();

    private void ConfigureReview(ModelBuilder mb)
    {
        mb.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
            e.HasIndex(x => x.ProductId);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Content).HasMaxLength(2000);
        });

        mb.Entity<ReviewImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).HasMaxLength(2000).IsRequired();
            e.HasOne(x => x.Review).WithMany(x => x.Images)
             .HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
