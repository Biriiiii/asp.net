using BookStore.Application.DTOs.Auth;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Application.Services;

// ── UserService ───────────────────────────────────────────

public class UserService : IUserService
{
    private readonly IUserRepository    _users;
    private readonly IAddressRepository _addresses;
    private readonly IUserSessionRepository _sessions;

    private const int MaxAddresses = 10;

    public UserService(
        IUserRepository users,
        IAddressRepository addresses,
        IUserSessionRepository sessions)
    {
        _users     = users;
        _addresses = addresses;
        _sessions  = sessions;
    }

    // ── Profile ───────────────────────────────────────────

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _users.GetWithRolesAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");
        return MapProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại.");

        // Kiểm tra SĐT trùng (nếu thay đổi)
        if (!string.IsNullOrEmpty(req.Phone) &&
            req.Phone != user.Phone &&
            await _users.PhoneExistsAsync(req.Phone, userId))
        {
            throw new InvalidOperationException("Số điện thoại đã được sử dụng bởi tài khoản khác.");
        }

        // Nếu thay SĐT → reset PhoneVerified
        if (!string.IsNullOrEmpty(req.Phone) && req.Phone != user.Phone)
            user.PhoneVerified = false;

        user.FullName    = req.FullName.Trim();
        user.Phone       = req.Phone?.Trim();
        user.DateOfBirth = req.DateOfBirth;
        user.Gender      = req.Gender;
        user.AvatarUrl   = req.AvatarUrl;
        user.UpdatedAt   = DateTime.UtcNow;

        _users.Update(user);
        await _users.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    // ── Addresses ─────────────────────────────────────────

    public async Task<IEnumerable<AddressDto>> GetAddressesAsync(Guid userId)
    {
        var items = await _addresses.GetByUserAsync(userId);
        return items.Select(MapAddress);
    }

    public async Task<AddressDto> GetAddressByIdAsync(Guid userId, Guid addressId)
    {
        var addr = await _addresses.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");

        if (addr.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập địa chỉ này.");

        return MapAddress(addr);
    }

    public async Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressRequest req)
    {
        var count = await _addresses.CountByUserAsync(userId);
        if (count >= MaxAddresses)
            throw new InvalidOperationException($"Tối đa {MaxAddresses} địa chỉ mỗi tài khoản.");

        // Nếu là default → clear default hiện tại
        if (req.IsDefault)
            await _addresses.ClearDefaultAsync(userId);

        // Địa chỉ đầu tiên tự động là default
        var isDefault = req.IsDefault || count == 0;

        var address = new UserAddress
        {
            UserId        = userId,
            RecipientName = req.RecipientName.Trim(),
            Phone         = req.Phone.Trim(),
            Province      = req.Province.Trim(),
            District      = req.District.Trim(),
            Ward          = req.Ward.Trim(),
            AddressLine   = req.AddressLine.Trim(),
            IsDefault     = isDefault
        };

        await _addresses.AddAsync(address);
        await _addresses.SaveChangesAsync();
        return MapAddress(address);
    }

    public async Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest req)
    {
        var addr = await _addresses.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");

        if (addr.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa địa chỉ này.");

        if (req.IsDefault && !addr.IsDefault)
            await _addresses.ClearDefaultAsync(userId);

        addr.RecipientName = req.RecipientName.Trim();
        addr.Phone         = req.Phone.Trim();
        addr.Province      = req.Province.Trim();
        addr.District      = req.District.Trim();
        addr.Ward          = req.Ward.Trim();
        addr.AddressLine   = req.AddressLine.Trim();
        addr.IsDefault     = req.IsDefault;
        addr.UpdatedAt     = DateTime.UtcNow;

        _addresses.Update(addr);
        await _addresses.SaveChangesAsync();
        return MapAddress(addr);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var addr = await _addresses.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");

        if (addr.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa địa chỉ này.");

        if (addr.IsDefault)
        {
            // Tự động chọn địa chỉ khác làm default
            var remaining = (await _addresses.GetByUserAsync(userId))
                .Where(a => a.Id != addressId)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefault();

            if (remaining != null)
            {
                remaining.IsDefault = true;
                _addresses.Update(remaining);
            }
        }

        _addresses.Delete(addr);
        await _addresses.SaveChangesAsync();
    }

    public async Task SetDefaultAddressAsync(Guid userId, Guid addressId)
    {
        var addr = await _addresses.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException("Địa chỉ không tồn tại.");

        if (addr.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác địa chỉ này.");

        await _addresses.ClearDefaultAsync(userId);
        addr.IsDefault = true;
        _addresses.Update(addr);
        await _addresses.SaveChangesAsync();
    }

    // ── Sessions ──────────────────────────────────────────

    public async Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(Guid userId)
    {
        var sessions = await _sessions.GetActiveByUserAsync(userId);
        return sessions.Select(s => new SessionDto(
            s.Id, s.IpAddress, s.UserAgent,
            s.CreatedAt, s.ExpiresAt, s.IsActive));
    }

    // ── Mappers ───────────────────────────────────────────

    private static UserProfileDto MapProfile(User u) =>
        new(u.Id, u.Email, u.FullName, u.Phone, u.DateOfBirth,
            u.Gender.ToString(), u.AvatarUrl,
            u.EmailVerified, u.PhoneVerified, u.LoyaltyPoints,
            u.UserRoles.Select(r => r.Role.ToString()),
            u.LastLoginAt, u.CreatedAt);

    private static AddressDto MapAddress(UserAddress a) =>
        new(a.Id, a.RecipientName, a.Phone,
            a.Province, a.District, a.Ward, a.AddressLine, a.IsDefault);
}

// ── AdminUserService ──────────────────────────────────────

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _users;

    public AdminUserService(IUserRepository users) => _users = users;

    public async Task<PagedResult<UserSummaryDto>> GetPagedAsync(UserQueryParams query)
    {
        var filter = new UserFilter
        {
            Keyword      = query.Keyword,
            Role         = query.Role,
            IsActive     = query.IsActive,
            EmailVerified= query.EmailVerified,
            SortBy       = query.SortBy,
            Page         = query.Page,
            PageSize     = query.PageSize
        };
        var (items, total) = await _users.GetPagedAsync(filter);
        var dtos = items.Select(u => new UserSummaryDto(
            u.Id, u.FullName, u.Email, u.Phone, u.AvatarUrl,
            u.IsActive,
            u.UserRoles.Select(r => r.Role.ToString()),
            u.CreatedAt));
        return new PagedResult<UserSummaryDto>(dtos, total, query.Page, query.PageSize);
    }

    public async Task<UserProfileDto> GetByIdAsync(Guid id)
    {
        var user = await _users.GetWithRolesAsync(id)
            ?? throw new KeyNotFoundException($"Người dùng Id={id} không tồn tại.");
        return MapProfile(user);
    }

    public async Task<UserProfileDto> CreateUserAsync(AdminCreateUserRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            throw new InvalidOperationException("Email đã được sử dụng.");

        var user = new User
        {
            Email         = req.Email.Trim().ToLower(),
            PasswordHash  = HashPassword(req.Password),
            FullName      = req.FullName.Trim(),
            Phone         = req.Phone?.Trim(),
            EmailVerified = true,   // Admin tạo thì không cần xác minh
            IsActive      = true
        };

        foreach (var role in req.Roles.Distinct())
            user.UserRoles.Add(new UserRole_ { UserId = user.Id, Role = role });

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();
        return MapProfile(user);
    }

    public async Task LockUserAsync(Guid id, int minutes)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Người dùng Id={id} không tồn tại.");
        user.LockoutUntil = DateTime.UtcNow.AddMinutes(minutes);
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    public async Task UnlockUserAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Người dùng Id={id} không tồn tại.");
        user.LockoutUntil     = null;
        user.FailedLoginCount = 0;
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    public async Task AssignRoleAsync(Guid userId, AssignRoleRequest req)
    {
        var user = await _users.GetWithRolesAsync(userId)
            ?? throw new KeyNotFoundException($"Người dùng Id={userId} không tồn tại.");

        if (user.UserRoles.Any(r => r.Role == req.Role))
            throw new InvalidOperationException($"Người dùng đã có role {req.Role}.");

        user.UserRoles.Add(new UserRole_ { UserId = userId, Role = req.Role });
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(Guid userId, UserRole role)
    {
        var user = await _users.GetWithRolesAsync(userId)
            ?? throw new KeyNotFoundException($"Người dùng Id={userId} không tồn tại.");

        var userRole = user.UserRoles.FirstOrDefault(r => r.Role == role)
            ?? throw new InvalidOperationException($"Người dùng không có role {role}.");

        if (user.UserRoles.Count == 1)
            throw new InvalidOperationException("Phải giữ ít nhất 1 role cho người dùng.");

        user.UserRoles.Remove(userRole);
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Người dùng Id={id} không tồn tại.");
        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    public async Task ActivateAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Người dùng Id={id} không tồn tại.");
        user.IsActive  = true;
        user.UpdatedAt = DateTime.UtcNow;
        _users.Update(user);
        await _users.SaveChangesAsync();
    }

    private static UserProfileDto MapProfile(User u) =>
        new(u.Id, u.Email, u.FullName, u.Phone, u.DateOfBirth,
            u.Gender.ToString(), u.AvatarUrl,
            u.EmailVerified, u.PhoneVerified, u.LoyaltyPoints,
            u.UserRoles.Select(r => r.Role.ToString()),
            u.LastLoginAt, u.CreatedAt);

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "BookStore_Salt_2024"));
        return Convert.ToBase64String(bytes);
    }
}
