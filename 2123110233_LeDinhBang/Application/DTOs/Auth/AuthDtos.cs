using BookStore.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Auth;

// ══════════════════════════════════════════════════════════
//  RESPONSES
// ══════════════════════════════════════════════════════════

public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry,
    UserProfileDto User
);

public record UserProfileDto(
    Guid Id,
    string? Email,
    string FullName,
    string? Phone,
    DateTime? DateOfBirth,
    string Gender,
    string? AvatarUrl,
    bool EmailVerified,
    bool PhoneVerified,
    int LoyaltyPoints,
    IEnumerable<string> Roles,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public record UserSummaryDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string? AvatarUrl,
    bool IsActive,
    IEnumerable<string> Roles,
    DateTime CreatedAt
);

public record AddressDto(
    Guid Id,
    string RecipientName,
    string Phone,
    string Province,
    string District,
    string Ward,
    string AddressLine,
    bool IsDefault
);

public record SessionDto(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsActive
);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

// ══════════════════════════════════════════════════════════
//  REQUESTS — Auth
// ══════════════════════════════════════════════════════════

public class RegisterRequest
{
    [Required] [EmailAddress] [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required] [MinLength(8)] [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$",
        ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ và 1 số.")]
    public string Password { get; set; } = string.Empty;

    [Required] [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone] [MaxLength(15)]
    public string? Phone { get; set; }
}

public class LoginRequest
{
    [Required] [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class PhoneLoginRequest
{
    [Required] [Phone] [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required] [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class OAuthCallbackRequest
{
    [Required] public string Provider { get; set; } = string.Empty;  // google | facebook
    [Required] public string IdToken { get; set; } = string.Empty;
}

public class SendOtpRequest
{
    [Required] [Phone] [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required] public string Token { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required] [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; } = string.Empty;

    [Required] [MinLength(8)] [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$",
        ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ và 1 số.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required] [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;

    [Required] [MinLength(8)] [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$")]
    public string NewPassword { get; set; } = string.Empty;

    [Required] [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════════
//  REQUESTS — Profile
// ══════════════════════════════════════════════════════════

public class UpdateProfileRequest
{
    [Required] [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone] [MaxLength(15)]
    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;

    [Url] [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

// ══════════════════════════════════════════════════════════
//  REQUESTS — Address
// ══════════════════════════════════════════════════════════

public class CreateAddressRequest
{
    [Required] [MaxLength(200)]
    public string RecipientName { get; set; } = string.Empty;

    [Required] [Phone] [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string Ward { get; set; } = string.Empty;

    [Required] [MaxLength(500)]
    public string AddressLine { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
}

public class UpdateAddressRequest : CreateAddressRequest { }

// ══════════════════════════════════════════════════════════
//  REQUESTS — Admin
// ══════════════════════════════════════════════════════════

public class AdminCreateUserRequest
{
    [Required] [EmailAddress] [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required] [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required] [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Phone] [MaxLength(15)]
    public string? Phone { get; set; }

    public IList<UserRole> Roles { get; set; } = new List<UserRole> { UserRole.Customer };
}

public class AssignRoleRequest
{
    [Required] public UserRole Role { get; set; }
}

public class UserQueryParams
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailVerified { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
