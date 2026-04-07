using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Users;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Identity.Api.Data;
using PaymentGateway.Identity.Api.Services;

namespace PaymentGateway.Identity.Api.Tests;

public class UserServiceTests : IDisposable
{
    private readonly IdentityDbContext _db;
    private readonly TenantContext _tenantContext;
    private readonly UserService _userService;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IdentityDbContext(options);

        _tenantContext = new TenantContext();

        // Seed test data
        var org = new Organization
        {
            Id = _orgId,
            Name = "Test Org",
            Slug = "test-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = _userId,
            OrganizationId = _orgId,
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            FirstName = "Test",
            LastName = "Admin",
            Role = UserRole.CustomerAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        _db.Users.Add(user);
        _db.SaveChanges();

        // Set tenant context via reflection since SetFromClaimsPrincipal needs a ClaimsPrincipal
        SetTenantContext(_orgId, _userId);

        _userService = new UserService(_db, _tenantContext);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsCorrectDto()
    {
        var result = await _userService.GetCurrentUser();

        result.Should().NotBeNull();
        result.Id.Should().Be(_userId);
        result.Email.Should().Be("admin@test.com");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("Admin");
        result.Role.Should().Be(UserRole.CustomerAdmin);
    }

    [Fact]
    public async Task GetCurrentUser_NoAuth_ThrowsUnauthorized()
    {
        var tenantContext = new TenantContext();
        var service = new UserService(_db, tenantContext);

        var act = () => service.GetCurrentUser();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateProfile_UpdatesFirstAndLastName()
    {
        var request = new UpdateProfileRequest("Updated", "Name");

        var result = await _userService.UpdateProfile(request);

        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("Name");

        var dbUser = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == _userId);
        dbUser.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task ListOrgUsers_ReturnsOnlySameOrgUsers()
    {
        // Add a user in a different org
        var otherOrg = new Organization
        {
            Id = Guid.NewGuid(), Name = "Other Org", Slug = "other-org",
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(otherOrg);
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(), OrganizationId = otherOrg.Id, Email = "other@test.com",
            PasswordHash = "hash", FirstName = "Other", LastName = "User",
            Role = UserRole.CustomerUser, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _userService.ListOrgUsers();

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task InviteUser_CreatesUserWithCorrectRole()
    {
        var request = new InviteUserRequest("invited@test.com", "Invited", "User", UserRole.CustomerUser);

        var result = await _userService.InviteUser(request);

        result.Email.Should().Be("invited@test.com");
        result.Role.Should().Be(UserRole.CustomerUser);
        result.IsActive.Should().BeTrue();

        var dbUser = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == "invited@test.com");
        dbUser.OrganizationId.Should().Be(_orgId);
    }

    [Fact]
    public async Task InviteUser_DuplicateEmail_ThrowsInvalidOperation()
    {
        var request = new InviteUserRequest("admin@test.com", "Dup", "User", UserRole.CustomerUser);

        var act = () => _userService.InviteUser(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    private void SetTenantContext(Guid orgId, Guid userId)
    {
        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("org_id", orgId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
            }, "test"));
        _tenantContext.SetFromClaimsPrincipal(claims);
    }

    public void Dispose() => _db.Dispose();
}
