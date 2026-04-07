using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Users;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Identity.Api.Data;

namespace PaymentGateway.Identity.Api.Services;

public class UserService
{
    private readonly IdentityDbContext _db;
    private readonly TenantContext _tenantContext;

    public UserService(IdentityDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<UserDto> GetCurrentUser()
    {
        var userId = _tenantContext.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        return ToDto(user);
    }

    public async Task<UserDto> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = _tenantContext.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<List<UserDto>> ListOrgUsers()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization not set.");

        var users = await _db.Users
            .Where(u => u.OrganizationId == orgId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> InviteUser(InviteUserRequest request)
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization not set.");

        var existingUser = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            throw new InvalidOperationException("A user with this email already exists.");

        var tempPassword = Guid.NewGuid().ToString()[..12];

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ToDto(user);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.IsActive, user.CreatedAt);
}
