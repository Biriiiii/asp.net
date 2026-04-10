using BookStore.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BookStore.API.Middleware;

/// <summary>
/// Middleware gắn thông tin user hiện tại vào HttpContext.Items
/// để các service có thể dùng mà không cần inject IHttpContextAccessor
/// </summary>
public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IUserService userService)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var sub = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(sub, out var userId))
            {
                try
                {
                    var profile = await userService.GetProfileAsync(userId);
                    ctx.Items["CurrentUser"] = profile;
                    ctx.Items["CurrentUserId"] = userId;
                }
                catch
                {
                    // Bỏ qua nếu user không tồn tại (token cũ)
                }
            }
        }

        await _next(ctx);
    }
}

// Extension method để đăng ký middleware
public static class CurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();
}
