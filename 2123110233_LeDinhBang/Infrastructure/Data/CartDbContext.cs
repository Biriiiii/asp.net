using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<Cart>     Carts     => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    private void ConfigureCart(ModelBuilder mb)
    {
        mb.Entity<Cart>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionId).HasMaxLength(200);
            // Partial index: unique UserId khi không null
            e.HasIndex(x => x.UserId).HasFilter("[UserId] IS NOT NULL").IsUnique();
            e.HasIndex(x => x.SessionId).HasFilter("[SessionId] IS NOT NULL");
        });

        mb.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(15,2)");
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Cart)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.CartId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
