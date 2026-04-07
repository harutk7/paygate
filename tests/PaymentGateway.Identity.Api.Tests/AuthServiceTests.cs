using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PaymentGateway.Contracts.Auth;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Identity.Api.Data;
using PaymentGateway.Identity.Api.Services;

namespace PaymentGateway.Identity.Api.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly IdentityDbContext _db;
    private readonly AuthService _authService;
    private readonly TokenService _tokenService;
    private const string TestJwtKey = "TestSecretKeyForUnitTests_MustBe32CharsMin!!";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IdentityDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey
            })
            .Build();

        _tokenService = new TokenService(config, _db);
        _authService = new AuthService(_db, _tokenService);
    }

    [Fact]
    public async Task Register_CreatesOrganizationAndUser()
    {
        var request = new RegisterRequest("Test Corp", "admin@test.com", "Pass123!", "John", "Doe");

        var result = await _authService.Register(request);

        result.Should().NotBeNull();
        result.User.Email.Should().Be("admin@test.com");
        result.User.FirstName.Should().Be("John");
        result.User.Role.Should().Be(UserRole.CustomerAdmin);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Name == "Test Corp");
        org.Should().NotBeNull();
        org!.Slug.Should().Be("test-corp");
    }

    [Fact]
    public async Task Register_HashesPassword()
    {
        var request = new RegisterRequest("Test Corp", "admin@test.com", "Pass123!", "John", "Doe");

        await _authService.Register(request);

        var user = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == "admin@test.com");
        user.PasswordHash.Should().NotBe("Pass123!");
        BCrypt.Net.BCrypt.Verify("Pass123!", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperation()
    {
        var request = new RegisterRequest("Corp 1", "dup@test.com", "Pass123!", "John", "Doe");
        await _authService.Register(request);

        var act = () => _authService.Register(
            new RegisterRequest("Corp 2", "dup@test.com", "Pass123!", "Jane", "Doe"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await _authService.Register(new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));

        var result = await _authService.Login(new LoginRequest("user@test.com", "Pass123!"));

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsUnauthorized()
    {
        await _authService.Register(new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));

        var act = () => _authService.Login(new LoginRequest("user@test.com", "WrongPass!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_NonExistentUser_ThrowsUnauthorized()
    {
        var act = () => _authService.Login(new LoginRequest("nobody@test.com", "Pass123!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_DeactivatedUser_ThrowsUnauthorized()
    {
        await _authService.Register(new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));
        var user = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == "user@test.com");
        user.IsActive = false;
        await _db.SaveChangesAsync();

        var act = () => _authService.Login(new LoginRequest("user@test.com", "Pass123!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var registerResult = await _authService.Register(
            new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));

        var result = await _authService.Refresh(new RefreshTokenRequest(registerResult.RefreshToken));

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(registerResult.RefreshToken); // rotated
    }

    [Fact]
    public async Task Refresh_RotatesToken_OldTokenInvalid()
    {
        var registerResult = await _authService.Register(
            new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));
        var oldToken = registerResult.RefreshToken;

        await _authService.Refresh(new RefreshTokenRequest(oldToken));

        var act = () => _authService.Refresh(new RefreshTokenRequest(oldToken));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Refresh_InvalidToken_ThrowsUnauthorized()
    {
        var act = () => _authService.Refresh(new RefreshTokenRequest("invalid-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var registerResult = await _authService.Register(
            new RegisterRequest("Test Corp", "user@test.com", "Pass123!", "Test", "User"));

        await _authService.Logout(registerResult.RefreshToken);

        var token = await _db.RefreshTokens.FirstAsync(t => t.Token == registerResult.RefreshToken);
        token.RevokedAt.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
