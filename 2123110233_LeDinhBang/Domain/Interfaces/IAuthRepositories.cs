using BookStore.Domain.Entities;
using BookStore.Domain.Enums;

namespace BookStore.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetWithRolesAsync(Guid id);
    Task<User?> GetWithAddressesAsync(Guid id);
    Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(UserFilter filter);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> PhoneExistsAsync(string phone, Guid? excludeId = null);
    Task AddAsync(User user);
    void Update(User user);
    Task<int> SaveChangesAsync();
}

public interface IUserSessionRepository
{
    Task<UserSession?> GetByTokenAsync(string refreshToken);
    Task<IEnumerable<UserSession>> GetActiveByUserAsync(Guid userId);
    Task AddAsync(UserSession session);
    void Update(UserSession session);
    Task RevokeAllByUserAsync(Guid userId);
    Task<int> SaveChangesAsync();
}

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetAsync(LoginProvider provider, string providerKey);
    Task<IEnumerable<ExternalLogin>> GetByUserAsync(Guid userId);
    Task AddAsync(ExternalLogin login);
    void Delete(ExternalLogin login);
    Task<int> SaveChangesAsync();
}

public interface IOtpRepository
{
    Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose);
    Task AddAsync(OtpCode otp);
    void Update(OtpCode otp);
    Task InvalidatePreviousAsync(Guid userId, OtpPurpose purpose);
    Task<int> SaveChangesAsync();
}

public interface IAddressRepository
{
    Task<UserAddress?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserAddress>> GetByUserAsync(Guid userId);
    Task<int> CountByUserAsync(Guid userId);
    Task AddAsync(UserAddress address);
    void Update(UserAddress address);
    void Delete(UserAddress address);
    Task ClearDefaultAsync(Guid userId);
    Task<int> SaveChangesAsync();
}

// Filter model cho admin
public class UserFilter
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailVerified { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
