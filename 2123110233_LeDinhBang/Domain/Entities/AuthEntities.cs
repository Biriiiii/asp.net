using BookStore.Domain.Enums;

namespace BookStore.Domain.Entities;

// ── UserAddress ───────────────────────────────────────────
public class UserAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    public User User { get; set; } = null!;
}

// ── UserSession (Refresh Token) ───────────────────────────
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}

// ── ExternalLogin (OAuth: Google, Facebook) ───────────────
public class ExternalLogin : BaseEntity
{
    public Guid UserId { get; set; }
    public LoginProvider Provider { get; set; }
    public string ProviderKey { get; set; } = string.Empty;   // Sub/ID từ provider
    public string? ProviderDisplayName { get; set; }
    public string? AccessToken { get; set; }

    public User User { get; set; } = null!;
}

// ── OtpCode (Email verify, Phone verify, Password reset) ─
public class OtpCode : BaseEntity
{
    public Guid UserId { get; set; }
    public OtpPurpose Purpose { get; set; }
    public string Code { get; set; } = string.Empty;       // Hashed
    public string? Target { get; set; }                    // Email hoặc SĐT
    public bool IsUsed { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired && AttemptCount < 5;
}
