using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Đăng ký tài khoản mới bằng email</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthTokenDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return StatusCode(201, result);
    }

    /// <summary>Đăng nhập bằng email + password</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Đăng nhập bằng số điện thoại + OTP</summary>
    [HttpPost("login/phone")]
    [ProducesResponseType(typeof(AuthTokenDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LoginWithPhone([FromBody] PhoneLoginRequest request)
    {
        var result = await _auth.LoginWithPhoneAsync(request);
        return Ok(result);
    }

    /// <summary>Gửi OTP về số điện thoại</summary>
    [HttpPost("otp/send")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        await _auth.SendPhoneOtpAsync(request);
        return Ok(new { message = "OTP đã được gửi." });
    }

    /// <summary>Đăng nhập qua Google hoặc Facebook</summary>
    [HttpPost("oauth")]
    [ProducesResponseType(typeof(AuthTokenDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> OAuth([FromBody] OAuthCallbackRequest request)
    {
        var result = await _auth.LoginWithOAuthAsync(request);
        return Ok(result);
    }

    /// <summary>Làm mới access token bằng refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _auth.RefreshTokenAsync(request);
        return Ok(result);
    }

    /// <summary>Đăng xuất (thu hồi refresh token)</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _auth.RevokeTokenAsync(request.RefreshToken);
        return Ok(new { message = "Đăng xuất thành công." });
    }

    /// <summary>Đăng xuất khỏi tất cả thiết bị</summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = GetCurrentUserId();
        await _auth.RevokeAllTokensAsync(userId);
        return Ok(new { message = "Đã đăng xuất khỏi tất cả thiết bị." });
    }

    /// <summary>Gửi email xác minh</summary>
    [HttpPost("email/send-verification")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SendVerification()
    {
        var userId = GetCurrentUserId();
        await _auth.SendEmailVerificationAsync(userId);
        return Ok(new { message = "Email xác minh đã được gửi." });
    }

    /// <summary>Xác minh email bằng token</summary>
    [HttpPost("email/verify")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _auth.VerifyEmailAsync(request);
        return Ok(new { message = "Xác minh email thành công." });
    }

    /// <summary>Yêu cầu đặt lại mật khẩu (gửi link về email)</summary>
    [HttpPost("password/forgot")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request);
        // Luôn trả 200 để tránh enumeration attack
        return Ok(new { message = "Nếu email tồn tại, link đặt lại mật khẩu đã được gửi." });
    }

    /// <summary>Đặt lại mật khẩu bằng token từ email</summary>
    [HttpPost("password/reset")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _auth.ResetPasswordAsync(request);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    /// <summary>Đổi mật khẩu khi đang đăng nhập</summary>
    [HttpPost("password/change")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        await _auth.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);
}
