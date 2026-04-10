using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category>       Categories       => Set<Category>();
    public DbSet<Publisher>      Publishers       => Set<Publisher>();
    public DbSet<Author>         Authors          => Set<Author>();
    public DbSet<Product>        Products         => Set<Product>();
    public DbSet<ProductAuthor>  ProductAuthors   => Set<ProductAuthor>();
    public DbSet<ProductImage>   ProductImages    => Set<ProductImage>();
    public DbSet<Inventory>      Inventories      => Set<Inventory>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Category (self-referencing tree) ──────────────
        mb.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Parent)
             .WithMany(x => x.Children)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Publisher ──────────────────────────────────────
        mb.Entity<Publisher>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        // ── Author ─────────────────────────────────────────
        mb.Entity<Author>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        // ── Product ────────────────────────────────────────
        mb.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Isbn).HasMaxLength(20);
            e.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("vi");
            e.Property(x => x.CoverType).HasConversion<string>();
            e.Property(x => x.OriginalPrice).HasColumnType("decimal(15,2)");
            e.Property(x => x.SalePrice).HasColumnType("decimal(15,2)");

            e.HasOne(x => x.Category)
             .WithMany(x => x.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Publisher)
             .WithMany(x => x.Products)
             .HasForeignKey(x => x.PublisherId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ProductAuthor (composite PK) ───────────────────
        mb.Entity<ProductAuthor>(e =>
        {
            e.HasKey(x => new { x.ProductId, x.AuthorId });
            e.Property(x => x.Role).HasMaxLength(50).HasDefaultValue("Author");

            e.HasOne(x => x.Product)
             .WithMany(x => x.ProductAuthors)
             .HasForeignKey(x => x.ProductId);

            e.HasOne(x => x.Author)
             .WithMany(x => x.ProductAuthors)
             .HasForeignKey(x => x.AuthorId);
        });

        // ── ProductImage ───────────────────────────────────
        mb.Entity<ProductImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AltText).HasMaxLength(300);

            e.HasOne(x => x.Product)
             .WithMany(x => x.Images)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Inventory (1-1 with Product) ───────────────────
        mb.Entity<Inventory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId).IsUnique();
            e.Ignore(x => x.QtyActual);     // computed property
            e.Ignore(x => x.IsLowStock);
            e.Ignore(x => x.IsOutOfStock);

            e.HasOne(x => x.Product)
             .WithOne(x => x.Inventory)
             .HasForeignKey<Inventory>(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Global query filters ───────────────────────────
        mb.Entity<Product>().HasQueryFilter(p => p.IsActive);
        mb.Entity<Category>().HasQueryFilter(c => c.IsActive);
    }

    // Auto-set UpdatedAt on SaveChanges
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
