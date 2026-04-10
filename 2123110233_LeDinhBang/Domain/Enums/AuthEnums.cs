namespace BookStore.Domain.Enums;

public enum Gender
{
    Male,
    Female,
    Other,
    Unspecified
}
public enum UserRole
{
    Customer,
    Staff,
    ContentManager,
    Marketing,
    Admin,
    SuperAdmin
}

public enum LoginProvider
{
    Local,      // Email + Password
    Google,
    Facebook,
    Phone       // SĐT + OTP
}

public enum OtpPurpose
{
    EmailVerification,
    PhoneVerification,
    PasswordReset,
    Login
}
