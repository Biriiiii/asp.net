using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public partial class AppDbContext
{
    public DbSet<Order>          Orders          => Set<Order>();
    public DbSet<OrderItem>      OrderItems      => Set<OrderItem>();
    public DbSet<Payment>        Payments        => Set<Payment>();
    public DbSet<Shipment>       Shipments       => Set<Shipment>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<RefundRequest>  RefundRequests  => Set<RefundRequest>();

    private void ConfigureOrder(ModelBuilder mb)
    {
        mb.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderCode).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ShippingMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ShippingFee).HasColumnType("decimal(15,2)");
            e.Property(x => x.Subtotal).HasColumnType("decimal(15,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(15,2)");
            e.Property(x => x.TotalAmount).HasColumnType("decimal(15,2)");
            e.Property(x => x.ShippingRecipientName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShippingPhone).HasMaxLength(15).IsRequired();
            e.Property(x => x.ShippingProvince).HasMaxLength(100).IsRequired();
            e.Property(x => x.ShippingDistrict).HasMaxLength(100).IsRequired();
            e.Property(x => x.ShippingWard).HasMaxLength(100).IsRequired();
            e.Property(x => x.ShippingAddressLine).HasMaxLength(500).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.Ignore(x => x.CanCancel);
        });

        mb.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(15,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(15,2)");
            e.Property(x => x.SnapshotTitle).HasMaxLength(500).IsRequired();
            e.Property(x => x.SnapshotIsbn).HasMaxLength(20);
            e.Property(x => x.SnapshotCoverUrl).HasMaxLength(2000);
            e.Property(x => x.SnapshotAuthorNames).HasMaxLength(500);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Order).WithMany(x => x.Items)
             .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            e.Property(x => x.TransactionId).HasMaxLength(200);
            e.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.Property(x => x.Amount).HasColumnType("decimal(15,2)");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.HasOne(x => x.Order).WithMany(x => x.Payments)
             .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Shipment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Carrier).HasMaxLength(50).IsRequired();
            e.Property(x => x.TrackingNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TrackingNumber);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.CarrierStatus).HasMaxLength(200);
            e.HasOne(x => x.Order).WithMany(x => x.Shipments)
             .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ShipmentTracking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Location).HasMaxLength(200);
            e.HasOne(x => x.Shipment).WithMany(x => x.TrackingEvents)
             .HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<OrderStatusLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.Order).WithMany(x => x.StatusLogs)
             .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<RefundRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId).IsUnique();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(15,2)");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.AdminNote).HasMaxLength(500);
            e.Property(x => x.TransactionId).HasMaxLength(200);
            e.HasOne(x => x.Order).WithOne(x => x.RefundRequest)
             .HasForeignKey<RefundRequest>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
