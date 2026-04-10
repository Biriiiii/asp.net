using BookStore.API.Extensions;
using BookStore.Application.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Cấu hình Services (DI Container) ──────────────────
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuthDatabase(builder.Configuration);

builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IOAuthService, MockOAuthService>();

builder.Services.AddRepositories();
builder.Services.AddAuthRepositories();
builder.Services.AddProductServices();
builder.Services.AddAuthServices();

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
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
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
        var appDb = services.GetRequiredService<AppDbContext>();
        var authDb = services.GetRequiredService<AuthDbContext>();

        await appDb.Database.MigrateAsync();
        await authDb.Database.MigrateAsync();

        await DbSeeder.SeedAsync(appDb, authDb);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi seed dữ liệu.");
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

// ── 5. Mock Services ──────────────────────────────────────
public class MockEmailService : IEmailService
{
    public Task SendEmailVerificationAsync(string toEmail, string fullName, string token) => Task.CompletedTask;
    public Task SendPasswordResetAsync(string toEmail, string fullName, string token) => Task.CompletedTask;
    public Task SendOrderConfirmationAsync(string toEmail, string fullName, string orderCode) => Task.CompletedTask;
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