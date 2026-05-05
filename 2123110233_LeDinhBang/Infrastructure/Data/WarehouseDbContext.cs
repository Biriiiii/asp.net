using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<Supplier>          Suppliers          => Set<Supplier>();
    public DbSet<PurchaseOrder>     PurchaseOrders     => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    private void ConfigureWarehouse(ModelBuilder mb)
    {
        mb.Entity<Supplier>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(15);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.TaxCode).HasMaxLength(20);
            e.HasIndex(x => x.Name);
        });

        mb.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PoNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.PoNumber).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(15,2)");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.Supplier)
             .WithMany(x => x.PurchaseOrders)
             .HasForeignKey(x => x.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PurchaseOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCost).HasColumnType("decimal(15,2)");
            e.Property(x => x.Note).HasMaxLength(200);
            e.Ignore(x => x.LineTotal);
            e.Ignore(x => x.QtyPending);
            e.HasOne(x => x.PurchaseOrder)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.PoId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
