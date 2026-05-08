using BookStore.API.Extensions;
using BookStore.Application.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Cấu hình Services (DI Container) ──────────────────
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuthDatabase(builder.Configuration);

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IOAuthService, MockOAuthService>();

builder.Services.AddRepositories();
builder.Services.AddAuthRepositories();
builder.Services.AddProductServices();
builder.Services.AddAuthServices();
builder.Services.AddCartModule();
builder.Services.AddOrderModule();
builder.Services.AddPromotionModule();
// builder.Services.AddReviewModule();
builder.Services.AddWarehouseModule();
builder.Services.AddCloudinaryModule(builder.Configuration);
builder.Services.AddDashboardModule();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 5 * 1024 * 1024);
// ── Auth (JWT) ────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "b8f9a2c4d6e8b1a3f5c7d9e0b2a4c6e8f0a1b3c5d7e9f1a2b4c6";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookStoreAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BookStoreClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BookStore API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập JWT token: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// ── 2. Build Ứng dụng ─────────────────────────────────────
var app = builder.Build();

// ── 3. Tự động Seed dữ liệu (Phải nằm SAU builder.Build()) ──
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var authDb = services.GetRequiredService<AuthDbContext>();
        var appDb = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Đang kiểm tra và cập nhật Database...");

        // Gọi hàm Seeder đã viết ở trên
        await DbSeeder.SeedAsync(authDb, appDb);

        logger.LogInformation("Cập nhật Database thành công!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra trong quá trình tự động tạo Database.");
    }
}

// ── 4. Middleware Pipeline ───────────────────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore API v1"));

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ── 5. Infrastructure Services ────────────────────────────
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string toEmail, string fullName, string token) =>
        SendAsync(toEmail, "Xác minh email BookStore", BuildEmailVerificationBody(fullName, token));

    public Task SendPasswordResetAsync(string toEmail, string fullName, string token) =>
        SendAsync(toEmail, "Đặt lại mật khẩu BookStore", BuildPasswordResetBody(fullName, token));

    public Task SendOrderConfirmationAsync(
        string toEmail,
        string fullName,
        string orderCode,
        string paymentStatus,
        decimal totalAmount,
        IEnumerable<string> orderItems) =>
        SendAsync(toEmail, $"Xác nhận đơn hàng {orderCode}", BuildOrderConfirmationBody(fullName, orderCode, paymentStatus, totalAmount, orderItems));

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = _configuration["Email:Smtp:Host"];
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var from = _configuration["Email:Smtp:From"] ?? username;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("Chưa cấu hình Email:Smtp nên không thể gửi email tới {Email}.", toEmail);
            return;
        }

        var port = _configuration.GetValue("Email:Smtp:Port", 587);
        var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true);
        var timeout = _configuration.GetValue("Email:Smtp:Timeout", 5000);
        var fromName = _configuration["Email:Smtp:FromName"] ?? "BookStore";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Timeout = timeout,
            Credentials = new NetworkCredential(username, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);
        await client.SendMailAsync(message);
    }

    private string BuildPasswordResetBody(string fullName, string token)
    {
        var resetUrl = _configuration["Frontend:ResetPasswordUrl"];
        var resetContent = string.IsNullOrWhiteSpace(resetUrl)
            ? $"<p>Mã đặt lại mật khẩu của bạn:</p><p style=\"font-size:18px;font-weight:bold\">{WebUtility.HtmlEncode(token)}</p>"
            : $"""
                <p>Mã OTP đặt lại mật khẩu của bạn:</p>
                <p style="font-size:28px;font-weight:bold;letter-spacing:4px">{WebUtility.HtmlEncode(token)}</p>
                <p>Hoặc bấm vào link sau để đặt lại mật khẩu:</p>
                <p><a href="{WebUtility.HtmlEncode(BuildResetUrl(resetUrl, token))}">Đặt lại mật khẩu</a></p>
                """;

        return $"""
            <p>Xin chào {WebUtility.HtmlEncode(fullName)},</p>
            <p>BookStore vừa nhận yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            {resetContent}
            <p>Mã OTP này sẽ hết hạn sau 30 phút. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
            """;
    }

    private static string BuildEmailVerificationBody(string fullName, string token) =>
        $"""
        <p>Xin chào {WebUtility.HtmlEncode(fullName)},</p>
        <p>Mã xác minh email BookStore của bạn:</p>
        <p style="font-size:18px;font-weight:bold">{WebUtility.HtmlEncode(token)}</p>
        """;

    private static string BuildOrderConfirmationBody(
        string fullName,
        string orderCode,
        string paymentStatus,
        decimal totalAmount,
        IEnumerable<string> orderItems)
    {
        var itemsHtml = string.Join("", orderItems.Select(item =>
            $"<li>{WebUtility.HtmlEncode(item)}</li>"));

        return $"""
            <p>Xin chào {WebUtility.HtmlEncode(fullName)},</p>
            <p>BookStore đã ghi nhận đơn hàng <strong>{WebUtility.HtmlEncode(orderCode)}</strong>.</p>
            <p>Trạng thái thanh toán: <strong>{WebUtility.HtmlEncode(paymentStatus)}</strong></p>
            <p>Tổng tiền: <strong>{totalAmount:N0} VND</strong></p>
            <p>Sản phẩm:</p>
            <ul>{itemsHtml}</ul>
            <p>Cảm ơn bạn đã đặt hàng tại BookStore.</p>
            """;
    }

    private static string BuildResetUrl(string resetUrl, string token)
    {
        var separator = resetUrl.Contains('?') ? "&" : "?";
        return $"{resetUrl}{separator}token={Uri.EscapeDataString(token)}";
    }
}

public class MockSmsService : ISmsService
{
    public Task SendOtpAsync(string phone, string otp) => Task.CompletedTask;
}

public class MockOAuthService : IOAuthService
{
    public Task<BookStore.Application.Interfaces.OAuthUserInfo> ValidateGoogleTokenAsync(string idToken) => throw new NotImplementedException();
    public Task<BookStore.Application.Interfaces.OAuthUserInfo> ValidateFacebookTokenAsync(string accessToken) => throw new NotImplementedException();
}
