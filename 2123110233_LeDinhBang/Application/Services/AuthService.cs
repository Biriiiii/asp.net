using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository        _users;
    private readonly IUserSessionRepository _sessions;
    private readonly IExternalLoginRepository _externalLogins;
    private readonly IOtpRepository         _otps;
    private readonly ITokenService          _tokens;
    private readonly IEmailService          _email;
    private readonly ISmsService            _sms;
    private readonly IOAuthService          _oauth;
    private readonly IConfiguration         _config;

    private const int MaxFailedAttempts  = 5;
    private const int LockoutMinutes     = 30;
    private const int RefreshTokenDays   = 30;
    private const int OtpExpiryMinutes   = 5;
    private const int ResetTokenMinutes  = 30;

    public AuthService(
        IUserRepository users,
        IUserSessionRepository sessions,
        IExternalLoginRepository externalLogins,
        IOtpRepository otps,
        ITokenService tokens,
        IEmailService email,
        ISmsService sms,
        IOAuthService oauth,
        IConfiguration config)
    {
        _users          = users;
        _sessions       = sessions;
        _externalLogins = externalLogins;
        _otps           = otps;
        _tokens         = tokens;
        _email          = email;
        _sms            = sms;
        _oauth          = oauth;
        _config         = config;
    }

    // ── Đăng ký ──────────────────────────────────────────

    public async Task<AuthTokenDto> RegisterAsync(RegisterRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            throw new InvalidOperationException("Email này đã được sử dụng.");

        if (!string.IsNullOrEmpty(req.Phone) && await _users.PhoneExistsAsync(req.Phone))
            throw new InvalidOperationException("Số điện thoại này đã được sử dụng.");

        var user = new User
        {
            Email        = req.Email.Trim().ToLower(),
            PasswordHash = HashPassword(req.Password),
            FullName     = req.FullName.Trim(),
            Phone        = req.Phone?.Trim(),
        };

        // Gán role mặc định Customer
        user.UserRoles.Add(new UserRole_ { UserId = user.Id, Role = UserRole.Customer });

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        // Gửi email xác minh
        await SendEmailVerificationAsync(user.Id);

        return await BuildAuthTokenAsync(user, req.Password.Length > 12);
    }

    // ── Đăng nhập Email + Password ────────────────────────

    public async Task<AuthTokenDto> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email.Trim().ToLower())
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa vĩnh viễn. Vui lòng liên hệ hỗ trợ.");

        if (user.IsLockedOut)
            throw new UnauthorizedAccessException(
                $"Tài khoản tạm khóa do nhập sai mật khẩu nhiều lần. Thử lại sau {Math.Ceiling((user.LockoutUntil!.Value - DateTime.UtcNow).TotalMinutes)} phút.");

        if (!VerifyPassword(req.Password, user.PasswordHash ?? ""))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutUntil     = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
            }
            _users.Update(user);
            await _users.SaveChangesAsync();
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        // Reset failed count
        user.FailedLoginCount = 0;
        user.LockoutUntil     = null;
        user.LastLoginAt      = DateTime.UtcNow;
        _users.Update(user);
        await _users.SaveChangesAsync();

        return await BuildAuthTokenAsync(user, req.RememberMe);
    }

    // ── Đăng nhập SĐT + OTP ──────────────────────────────

    public async Task<AuthTokenDto> LoginWithPhoneAsync(PhoneLoginRequest req)
    {
        var user = await _users.GetByPhoneAsync(req.Phone.Trim())
            ?? throw new UnauthorizedAccessException("Số điện thoại chưa được đăng ký.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        var otp = await _otps.GetLatestAsync(user.Id, OtpPurpose.Login)
            ?? throw new UnauthorizedAccessException("Mã OTP không hợp lệ hoặc đã hết hạn.");

        if (!otp.IsValid)
            throw new UnauthorizedAccessException("Mã OTP không hợp lệ hoặc đã hết hạn.");

        if (!VerifyOtp(req.Otp, otp.Code))
        {
            otp.AttemptCount++;
            _otps.Update(otp);
            await _otps.SaveChangesAsync();
            throw new UnauthorizedAccessException("Mã OTP không đúng.");
        }

        otp.IsUsed = true;
        _otps.Update(otp);
        user.LastLoginAt   = DateTime.UtcNow;
        user.PhoneVerified = true;
        _users.Update(user);
        await _users.SaveChangesAsync();

        return await BuildAuthTokenAsync(user, false);
    }

    // ── OAuth (Google / Facebook) ─────────────────────────

    public async Task<AuthTokenDto> LoginWithOAuthAsync(OAuthCallbackRequest req)
    {
        var provider = req.Provider.ToLower() switch
        {
            "google"   => LoginProvider.Google,
            "facebook" => LoginProvider.Facebook,
            _          => throw new ArgumentException("Provider không hợp lệ.")
        };

        var info = provider == LoginProvider.Google
            ? await _oauth.ValidateGoogleTokenAsync(req.IdToken)
            : await _oauth.ValidateFacebookTokenAsync(req.IdToken);

        // Kiểm tra external login đã tồn tại chưa
        var externalLogin = await _externalLogins.GetAsync(provider, info.ProviderKey);

        User user;
        if (externalLogin != null)
        {
            user = await _users.GetWithRolesAsync(externalLogin.UserId)
                ?? throw new InvalidOperationException("Tài khoản không tồn tại.");
        }
        else
        {
            // Kiểm tra email đã có local account không → liên kết
            user = info.Email != null
                ? await _users.GetByEmailAsync(info.Email) ?? await CreateOAuthUserAsync(info)
                : await CreateOAuthUserAsync(info);

            // Gắn external login
            await _externalLogins.AddAsync(new ExternalLogin
            {
                UserId              = user.Id,
                Provider            = provider,
                ProviderKey         = info.ProviderKey,
                ProviderDisplayName = info.FullName,
            });
            await _externalLogins.SaveChangesAsync();
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        user.LastLoginAt = DateTime.UtcNow;
        if (info.AvatarUrl != null && user.AvatarUrl == null)
            user.AvatarUrl = info.AvatarUrl;
        _users.Update(user);
        await _users.SaveChangesAsync();

        return await BuildAuthTokenAsync(user, false);
    }

    // ── Refresh Token ─────────────────────────────────────

    public async Task<AuthTokenDto> RefreshTokenAsync(RefreshTokenRequest req)
    {
        var session = await _sessions.GetByTokenAsync(req.RefreshToken)
            ?? throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        if (!session.IsActive)
            throw new UnauthorizedAccessException("Refresh token đã hết hạn hoặc bị thu hồi.");

        var user = await _users.GetWithRolesAsync(session.UserId)
            ?? throw new UnauthorizedAccessException("Tài khoản không tồn tại.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        // Rotate refresh token
        session.IsRevoked = true;
        _sessions.Update(session);

        return await BuildAuthTokenAsync(user, false);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var session = await _sessions.GetByTokenAsync(refreshToken);
        if (session == null) return;
        session.IsRevoked = true;
        _sessions.Update(session);
        await _sessions.SaveChangesAsync();
    }

    public async Task RevokeAllTokensAsync(Guid userId)
    {
        await _sessions.RevokeAllByUserAsync(userId);
        await _sessions.SaveChangesAsync();
    }

    // ── Email Verification ────────────────────────────────

    public async Task SendEmailVerificationAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        if (user.EmailVerified)
            throw new InvalidOperationException("Email đã được xác minh.");

        await _otps.InvalidatePreviousAsync(userId, OtpPurpose.EmailVerification);

        var rawToken = GenerateSecureToken();
        var otp = new OtpCode
        {
            UserId    = userId,
            Purpose   = OtpPurpose.EmailVerification,
            Code      = HashPassword(rawToken),
            Target    = user.Email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenMinutes)
        };
        await _otps.AddAsync(otp);
        await _otps.SaveChangesAsync();

        await _email.SendEmailVerificationAsync(user.Email!, user.FullName, rawToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest req)
    {
        // Token format: {userId}:{rawToken}
        var parts = req.Token.Split(':');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
            throw new InvalidOperationException("Token không hợp lệ.");

        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        if (user.EmailVerified)
            throw new InvalidOperationException("Email đã được xác minh trước đó.");

        var otp = await _otps.GetLatestAsync(userId, OtpPurpose.EmailVerification)
            ?? throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");

        if (!otp.IsValid || !VerifyPassword(parts[1], otp.Code))
            throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");

        otp.IsUsed        = true;
        user.EmailVerified = true;
        _otps.Update(otp);
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    // ── Phone OTP ─────────────────────────────────────────

    public async Task SendPhoneOtpAsync(SendOtpRequest req)
    {
        var user = await _users.GetByPhoneAsync(req.Phone.Trim())
            ?? throw new KeyNotFoundException("Số điện thoại chưa được đăng ký.");

        await _otps.InvalidatePreviousAsync(user.Id, OtpPurpose.Login);

        var rawOtp = GenerateNumericOtp(6);
        var otp = new OtpCode
        {
            UserId    = user.Id,
            Purpose   = OtpPurpose.Login,
            Code      = HashPassword(rawOtp),
            Target    = req.Phone,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes)
        };
        await _otps.AddAsync(otp);
        await _otps.SaveChangesAsync();

        await _sms.SendOtpAsync(req.Phone, rawOtp);
    }

    // ── Forgot / Reset Password ───────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email.Trim().ToLower());
        // Không báo lỗi nếu email không tồn tại (bảo mật)
        if (user == null || !user.IsActive) return;

        await _otps.InvalidatePreviousAsync(user.Id, OtpPurpose.PasswordReset);

        var rawToken = GenerateSecureToken();
        var otp = new OtpCode
        {
            UserId    = user.Id,
            Purpose   = OtpPurpose.PasswordReset,
            Code      = HashPassword(rawToken),
            Target    = user.Email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenMinutes)
        };
        await _otps.AddAsync(otp);
        await _otps.SaveChangesAsync();

        await _email.SendPasswordResetAsync(user.Email!, user.FullName, $"{user.Id}:{rawToken}");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req)
    {
        var parts = req.Token.Split(':');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
            throw new InvalidOperationException("Token không hợp lệ.");

        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        var otp = await _otps.GetLatestAsync(userId, OtpPurpose.PasswordReset)
            ?? throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");

        if (!otp.IsValid || !VerifyPassword(parts[1], otp.Code))
            throw new InvalidOperationException("Token không hợp lệ hoặc đã hết hạn.");

        otp.IsUsed        = true;
        user.PasswordHash  = HashPassword(req.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutUntil  = null;
        _otps.Update(otp);
        _users.Update(user);

        // Thu hồi tất cả session cũ
        await _sessions.RevokeAllByUserAsync(userId);
        await _users.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        if (!VerifyPassword(req.CurrentPassword, user.PasswordHash ?? ""))
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = HashPassword(req.NewPassword);
        _users.Update(user);

        // Thu hồi tất cả session ngoại trừ session hiện tại
        await _sessions.RevokeAllByUserAsync(userId);
        await _users.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────

    private async Task<AuthTokenDto> BuildAuthTokenAsync(User user, bool longLived)
    {
        var userWithRoles = await _users.GetWithRolesAsync(user.Id) ?? user;
        var roles         = userWithRoles.UserRoles.Select(r => r.Role.ToString()).ToList();

        var accessToken      = _tokens.GenerateAccessToken(user, roles);
        var rawRefreshToken   = _tokens.GenerateRefreshToken();
        var refreshExpiry     = DateTime.UtcNow.AddDays(longLived ? RefreshTokenDays : 1);

        var session = new UserSession
        {
            UserId        = user.Id,
            RefreshToken  = rawRefreshToken,
            ExpiresAt     = refreshExpiry
        };
        await _sessions.AddAsync(session);
        await _sessions.SaveChangesAsync();

        var accessExpiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

        return new AuthTokenDto(
            accessToken,
            rawRefreshToken,
            accessExpiry,
            refreshExpiry,
            MapProfile(userWithRoles, roles)
        );
    }

    private async Task<User> CreateOAuthUserAsync(OAuthUserInfo info)
    {
        var user = new User
        {
            Email         = info.Email?.Trim().ToLower(),
            FullName      = info.FullName.Trim(),
            AvatarUrl     = info.AvatarUrl,
            EmailVerified = info.EmailVerified,
            IsActive      = true,
        };
        user.UserRoles.Add(new UserRole_ { UserId = user.Id, Role = UserRole.Customer });
        await _users.AddAsync(user);
        await _users.SaveChangesAsync();
        return user;
    }

    private static UserProfileDto MapProfile(User u, IEnumerable<string> roles) =>
        new(u.Id, u.Email, u.FullName, u.Phone, u.DateOfBirth,
            u.Gender.ToString(), u.AvatarUrl,
            u.EmailVerified, u.PhoneVerified,
            u.LoyaltyPoints, roles, u.LastLoginAt, u.CreatedAt);

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "BookStore_Salt_2024"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;

    private static bool VerifyOtp(string raw, string hash) =>
        HashPassword(raw) == hash;

    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

    private static string GenerateNumericOtp(int length) =>
        string.Concat(Enumerable.Range(0, length)
            .Select(_ => RandomNumberGenerator.GetInt32(0, 10).ToString()));
}
