using System.Net;
using System.Text.Json;

namespace BookStore.API.Extensions;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(ctx, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            KeyNotFoundException    => (HttpStatusCode.NotFound,            ex.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest,        ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,    ex.Message),
            ArgumentException       => (HttpStatusCode.BadRequest,          ex.Message),
            _                       => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống.")
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status  = (int)statusCode,
            message,
            timestamp = DateTime.UtcNow
        });

        return ctx.Response.WriteAsync(body);
    }
}
