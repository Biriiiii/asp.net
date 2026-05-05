using System.Text.Json.Serialization;

namespace BookStore.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    Male,
    Female,
    Other,
    Unspecified
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Customer,
    Staff,
    ContentManager,
    Marketing,
    Admin,
    SuperAdmin
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoginProvider
{
    Local,      // Email + Password
    Google,
    Facebook,
    Phone       // SĐT + OTP
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OtpPurpose
{
    EmailVerification,
    PhoneVerification,
    PasswordReset,
    Login
}
