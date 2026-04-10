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
        // 1. Kiểm tra đầu vào
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new Exception("Email và mật khẩu không được để trống.");
        }

        // 2. Tìm người dùng theo Email (Sử dụng UserRepository đã tối ưu ToLower)
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // 3. Kiểm tra sự tồn tại của người dùng và tính hợp lệ của mật khẩu
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new Exception("Tài khoản không tồn tại hoặc thông tin đăng nhập không chính xác.");
        }

        // 4. Kiểm tra mật khẩu (Sử dụng hàm băm ComputeHash đồng bộ với DbSeeder)
        var hashAttempt = ComputeHash(request.Password);
        if (user.PasswordHash != hashAttempt)
        {
            throw new Exception("Mật khẩu không chính xác.");
        }

        // 5. Kiểm tra trạng thái tài khoản
        if (!user.IsActive)
        {
            throw new Exception("Tài khoản của bạn hiện đang bị khóa.");
        }

        // 6. Chuẩn bị danh sách Roles (Lấy từ bảng junction UserRoles)
        var roles = user.UserRoles?.Select(r => r.Role.ToString()).ToList() ?? new List<string> { "Customer" };

        // 7. Tạo Token và thời gian hết hạn
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Giả sử AccessToken hết hạn sau 60 phút, RefreshToken sau 7 ngày
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // 8. Mapping sang UserProfileDto (ĐÚNG 13 THAM SỐ THEO THỨ TỰ)
        var userProfile = new UserProfileDto(
            user.Id,                        // 1. Guid Id
            user.Email,                     // 2. string? Email
            user.FullName,                  // 3. string FullName
            user.Phone,                     // 4. string? Phone
            user.DateOfBirth,               // 5. DateTime? DateOfBirth
            user.Gender.ToString(),         // 6. string Gender (Chuyển Enum sang String)
            user.AvatarUrl,                 // 7. string? AvatarUrl
            user.EmailVerified,             // 8. bool EmailVerified
            user.PhoneVerified,             // 9. bool PhoneVerified
            user.LoyaltyPoints,             // 10. int LoyaltyPoints
            roles,                          // 11. IEnumerable<string> Roles
            user.LastLoginAt,               // 12. DateTime? LastLoginAt
            user.CreatedAt                  // 13. DateTime CreatedAt
        );

        // 9. Cập nhật thời gian đăng nhập cuối cùng
        user.LastLoginAt = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        // 10. Trả về AuthTokenDto hoàn chỉnh
        return new AuthTokenDto(
            accessToken,
            refreshToken,
            accessTokenExpiry,
            refreshTokenExpiry,
            userProfile
        );
    }

    // Hàm hỗ trợ băm mật khẩu (Đảm bảo dùng chung Salt với DbSeeder)
    private string ComputeHash(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;

        // Dùng mã Salt cố định để khớp với dữ liệu đã lưu trong DB
        const string salt = "BookStore_Salt_2024";

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + salt));
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