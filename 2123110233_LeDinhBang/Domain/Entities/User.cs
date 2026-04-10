using BookStore.Domain.Enums;

namespace BookStore.Domain.Entities;

public class User : BaseEntity
{
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    public int LoyaltyPoints { get; set; } = 0;

    // Bảo mật
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public ICollection<UserRole_> UserRoles { get; set; } = new List<UserRole_>();
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();

    // Computed
    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;
}

// Bảng junction User ↔ Role
public class UserRole_
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedBy { get; set; }

    public User User { get; set; } = null!;
}
