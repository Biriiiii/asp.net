using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly string _salt = "BookStore_Salt_2024";

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginRequest request)
    {
        // 1. Kiểm tra đầu vào cơ bản
        if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new Exception("Email và mật khẩu không được để trống.");

        // 2. Tìm User từ Repository
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // 3. Kiểm tra User và PasswordHash (Tránh lỗi Parameter 's' null)
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            throw new Exception("Email hoặc mật khẩu không đúng.");

        // 4. So sánh mật khẩu băm
        var hashAttempt = ComputeHash(request.Password);
        if (user.PasswordHash != hashAttempt)
            throw new Exception("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive) throw new Exception("Tài khoản đã bị khóa.");

        // 5. Chuẩn bị dữ liệu Token
        var roles = user.UserRoles?.Select(r => r.Role.ToString()).ToList() ?? new List<string> { "Customer" };
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var accessTokenExpiry = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // 6. Tạo UserProfileDto (Khớp đúng 13 tham số theo cấu trúc record của bạn)
        var userProfile = new UserProfileDto(
            user.Id,                        // 1. Guid Id
            user.Email,                     // 2. string? Email
            user.FullName,                  // 3. string FullName
            user.Phone,                     // 4. string? Phone
            user.DateOfBirth,               // 5. DateTime? DateOfBirth
            user.Gender.ToString(),         // 6. string Gender (Chuyển Enum sang string)
            user.AvatarUrl,                 // 7. string? AvatarUrl
            user.EmailVerified,             // 8. bool EmailVerified
            user.PhoneVerified,             // 9. bool PhoneVerified
            user.LoyaltyPoints,             // 10. int LoyaltyPoints
            roles,                          // 11. IEnumerable<string> Roles
            user.LastLoginAt,               // 12. DateTime? LastLoginAt
            user.CreatedAt                  // 13. DateTime CreatedAt
        );

        // 7. Tạo AuthTokenDto (Khớp đúng 5 tham số theo cấu trúc record của bạn)
        return new AuthTokenDto(
            accessToken,                    // 1. string AccessToken
            refreshToken,                   // 2. string RefreshToken
            accessTokenExpiry,              // 3. DateTime AccessTokenExpiry
            refreshTokenExpiry,             // 4. DateTime RefreshTokenExpiry
            userProfile                     // 5. UserProfileDto User
        );
    }

    private string ComputeHash(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + _salt));
        return Convert.ToBase64String(bytes);
    }

    // Các phương thức khác (Tạm thời throw để build được)
    public Task<AuthTokenDto> RegisterAsync(RegisterRequest request) => throw new NotImplementedException();
    public Task<AuthTokenDto> LoginWithPhoneAsync(PhoneLoginRequest request) => throw new NotImplementedException();
    public Task<AuthTokenDto> LoginWithOAuthAsync(OAuthCallbackRequest request) => throw new NotImplementedException();
    public Task<AuthTokenDto> RefreshTokenAsync(RefreshTokenRequest request) => throw new NotImplementedException();
    public Task RevokeTokenAsync(string refreshToken) => Task.CompletedTask;
    public Task RevokeAllTokensAsync(Guid userId) => Task.CompletedTask;
    public Task SendEmailVerificationAsync(Guid userId) => Task.CompletedTask;
    public Task VerifyEmailAsync(VerifyEmailRequest request) => Task.CompletedTask;
    public Task SendPhoneOtpAsync(SendOtpRequest request) => Task.CompletedTask;
    public Task ForgotPasswordAsync(ForgotPasswordRequest request) => Task.CompletedTask;
    public Task ResetPasswordAsync(ResetPasswordRequest request) => Task.CompletedTask;
    public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request) => Task.CompletedTask;
}