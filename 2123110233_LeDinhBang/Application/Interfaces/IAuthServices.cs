using BookStore.Application.DTOs.Auth;
using BookStore.Domain.Enums;

namespace BookStore.Application.Interfaces;

public interface IAuthService
{
    // Đăng ký / Đăng nhập
    Task<AuthTokenDto> RegisterAsync(RegisterRequest request);
    Task<AuthTokenDto> LoginAsync(LoginRequest request);
    Task<AuthTokenDto> LoginWithPhoneAsync(PhoneLoginRequest request);
    Task<AuthTokenDto> LoginWithOAuthAsync(OAuthCallbackRequest request);

    // Token
    Task<AuthTokenDto> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeTokenAsync(string refreshToken);
    Task RevokeAllTokensAsync(Guid userId);

    // Xác minh & đặt lại mật khẩu
    Task SendEmailVerificationAsync(Guid userId);
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task SendPhoneOtpAsync(SendOtpRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}

public interface IUserService
{
    // Profile
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

    // Địa chỉ
    Task<IEnumerable<AddressDto>> GetAddressesAsync(Guid userId);
    Task<AddressDto> GetAddressByIdAsync(Guid userId, Guid addressId);
    Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressRequest request);
    Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request);
    Task DeleteAddressAsync(Guid userId, Guid addressId);
    Task SetDefaultAddressAsync(Guid userId, Guid addressId);

    // Sessions
    Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(Guid userId);
}

public interface IAdminUserService
{
    Task<PagedResult<UserSummaryDto>> GetPagedAsync(UserQueryParams query);
    Task<UserProfileDto> GetByIdAsync(Guid id);
    Task<UserProfileDto> CreateUserAsync(AdminCreateUserRequest request);
    Task LockUserAsync(Guid id, int minutes);
    Task UnlockUserAsync(Guid id);
    Task AssignRoleAsync(Guid userId, AssignRoleRequest request);
    Task RemoveRoleAsync(Guid userId, UserRole role);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
}

public interface ITokenService
{
    string GenerateAccessToken(Domain.Entities.User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);        // Dùng cho refresh (expired token vẫn đọc được claims)
}

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string token);
    Task SendPasswordResetAsync(string toEmail, string fullName, string token);
    Task SendOrderConfirmationAsync(string toEmail, string fullName, string orderCode);
}

public interface ISmsService
{
    Task SendOtpAsync(string phone, string otp);
}

public interface IOAuthService
{
    Task<OAuthUserInfo> ValidateGoogleTokenAsync(string idToken);
    Task<OAuthUserInfo> ValidateFacebookTokenAsync(string accessToken);
}

public record OAuthUserInfo(
    string ProviderKey,
    string? Email,
    string FullName,
    string? AvatarUrl,
    bool EmailVerified
);
