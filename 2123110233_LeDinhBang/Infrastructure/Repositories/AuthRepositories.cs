using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repositories;

// ── UserRepository ────────────────────────────────────────

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> GetByPhoneAsync(string phone) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone);

    public async Task<User?> GetWithRolesAsync(Guid id) =>
        await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetWithAddressesAsync(Guid id) =>
        await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Addresses.OrderBy(a => a.CreatedAt))
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(UserFilter f)
    {
        var query = _db.Users.Include(u => u.UserRoles).AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var kw = f.Keyword.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(kw)) ||
                u.FullName.ToLower().Contains(kw) ||
                (u.Phone != null && u.Phone.Contains(kw)));
        }

        if (f.Role.HasValue)
            query = query.Where(u => u.UserRoles.Any(r => r.Role == f.Role.Value));

        if (f.IsActive.HasValue)
            query = query.Where(u => u.IsActive == f.IsActive.Value);

        if (f.EmailVerified.HasValue)
            query = query.Where(u => u.EmailVerified == f.EmailVerified.Value);

        query = f.SortBy switch
        {
            "name"    => query.OrderBy(u => u.FullName),
            "email"   => query.OrderBy(u => u.Email),
            _         => query.OrderByDescending(u => u.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null) =>
        await _db.Users.AnyAsync(u =>
            u.Email == email.ToLower() && (excludeId == null || u.Id != excludeId));

    public async Task<bool> PhoneExistsAsync(string phone, Guid? excludeId = null) =>
        await _db.Users.AnyAsync(u =>
            u.Phone == phone && (excludeId == null || u.Id != excludeId));

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);
    public void Update(User user)        => _db.Users.Update(user);
    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── UserSessionRepository ─────────────────────────────────

public class UserSessionRepository : IUserSessionRepository
{
    private readonly AuthDbContext _db;
    public UserSessionRepository(AuthDbContext db) => _db = db;

    public async Task<UserSession?> GetByTokenAsync(string refreshToken) =>
        await _db.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    public async Task<IEnumerable<UserSession>> GetActiveByUserAsync(Guid userId) =>
        await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(UserSession session) => await _db.UserSessions.AddAsync(session);
    public void Update(UserSession session)          => _db.UserSessions.Update(session);

    public async Task RevokeAllByUserAsync(Guid userId)
    {
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();
        foreach (var s in sessions)
            s.IsRevoked = true;
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── ExternalLoginRepository ───────────────────────────────

public class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly AuthDbContext _db;
    public ExternalLoginRepository(AuthDbContext db) => _db = db;

    public async Task<ExternalLogin?> GetAsync(LoginProvider provider, string providerKey) =>
        await _db.ExternalLogins
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey);

    public async Task<IEnumerable<ExternalLogin>> GetByUserAsync(Guid userId) =>
        await _db.ExternalLogins
            .Where(e => e.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddAsync(ExternalLogin login) => await _db.ExternalLogins.AddAsync(login);
    public void Delete(ExternalLogin login)          => _db.ExternalLogins.Remove(login);
    public async Task<int> SaveChangesAsync()        => await _db.SaveChangesAsync();
}

// ── OtpRepository ─────────────────────────────────────────

public class OtpRepository : IOtpRepository
{
    private readonly AuthDbContext _db;
    public OtpRepository(AuthDbContext db) => _db = db;

    public async Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose) =>
        await _db.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task AddAsync(OtpCode otp) => await _db.OtpCodes.AddAsync(otp);
    public void Update(OtpCode otp)          => _db.OtpCodes.Update(otp);

    public async Task InvalidatePreviousAsync(Guid userId, OtpPurpose purpose)
    {
        var otps = await _db.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
        foreach (var o in otps)
            o.IsUsed = true;
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}

// ── AddressRepository ─────────────────────────────────────

public class AddressRepository : IAddressRepository
{
    private readonly AuthDbContext _db;
    public AddressRepository(AuthDbContext db) => _db = db;

    public async Task<UserAddress?> GetByIdAsync(Guid id) =>
        await _db.UserAddresses.FindAsync(id);

    public async Task<IEnumerable<UserAddress>> GetByUserAsync(Guid userId) =>
        await _db.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<int> CountByUserAsync(Guid userId) =>
        await _db.UserAddresses.CountAsync(a => a.UserId == userId);

    public async Task AddAsync(UserAddress address) => await _db.UserAddresses.AddAsync(address);
    public void Update(UserAddress address)          => _db.UserAddresses.Update(address);
    public void Delete(UserAddress address)          => _db.UserAddresses.Remove(address);

    public async Task ClearDefaultAsync(Guid userId)
    {
        var defaults = await _db.UserAddresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync();
        foreach (var a in defaults)
            a.IsDefault = false;
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
}
