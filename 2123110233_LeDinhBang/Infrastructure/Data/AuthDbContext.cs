using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    // ── DbSet Definitions ──────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole_> UserRoles => Set<UserRole_>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── 1. Cấu hình bảng User ────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();

            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

            e.Property(x => x.Phone).HasMaxLength(15);
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");

            e.Property(x => x.AvatarUrl).HasMaxLength(500);

            // QUAN TRỌNG: Lưu Enum Gender dưới dạng String để tránh lỗi Cast và dễ đọc DB
            e.Property(x => x.Gender)
             .HasConversion<string>()
             .HasMaxLength(20)
             .IsRequired();

            e.Ignore(x => x.IsLockedOut);
        });

        // ── 2. Cấu hình bảng UserRole (Junction Table) ───────────
        mb.Entity<UserRole_>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(x => new { x.UserId, x.Role });

            // Lưu Enum Role dưới dạng String
            e.Property(x => x.Role)
             .HasConversion<string>()
             .HasMaxLength(50);

            e.HasOne(x => x.User)
             .WithMany(x => x.UserRoles)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 3. Cấu hình bảng UserAddress ──────────────────────────
        mb.Entity<UserAddress>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(15).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100).IsRequired();
            e.Property(x => x.District).HasMaxLength(100).IsRequired();
            e.Property(x => x.Ward).HasMaxLength(100).IsRequired();
            e.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();

            e.HasOne(x => x.User)
             .WithMany(x => x.Addresses)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 4. Cấu hình bảng UserSession ──────────────────────────
        mb.Entity<UserSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RefreshToken).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.RefreshToken).IsUnique();
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);

            e.Ignore(x => x.IsExpired);
            e.Ignore(x => x.IsActive);

            e.HasOne(x => x.User)
             .WithMany(x => x.Sessions)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 5. Cấu hình bảng ExternalLogin ────────────────────────
        mb.Entity<ExternalLogin>(e =>
        {
            e.HasKey(x => x.Id);

            // Lưu Enum Provider dưới dạng String
            e.Property(x => x.Provider)
             .HasConversion<string>()
             .HasMaxLength(50);

            e.Property(x => x.ProviderKey).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique();
            e.Property(x => x.ProviderDisplayName).HasMaxLength(200);

            e.HasOne(x => x.User)
             .WithMany(x => x.ExternalLogins)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── 6. Cấu hình bảng OtpCode ───────────────────────────────
        mb.Entity<OtpCode>(e =>
        {
            e.HasKey(x => x.Id);

            // Lưu Enum Purpose dưới dạng String
            e.Property(x => x.Purpose)
             .HasConversion<string>()
             .HasMaxLength(50);

            e.Property(x => x.Code).HasMaxLength(500).IsRequired();
            e.Property(x => x.Target).HasMaxLength(200);

            e.Ignore(x => x.IsExpired);
            e.Ignore(x => x.IsValid);

            e.HasOne(x => x.User)
             .WithMany(x => x.OtpCodes)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Tự động cập nhật thời gian chỉnh sửa (UpdatedAt)
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(ct);
    }
}