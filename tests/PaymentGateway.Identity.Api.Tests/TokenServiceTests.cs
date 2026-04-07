using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Identity.Api.Data;
using PaymentGateway.Identity.Api.Services;

namespace PaymentGateway.Identity.Api.Tests;

public class TokenServiceTests : IDisposable
{
    private readonly IdentityDbContext _db;
    private readonly TokenService _tokenService;
    private const string TestJwtKey = "TestSecretKeyForUnitTests_MustBe32CharsMin!!";

    public TokenServiceTests()
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
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var user = CreateTestUser();

        var tokenString = _tokenService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
        token.Claims.Should().Contain(c => c.Type == "org_id" && c.Value == user.OrganizationId.ToString());
        token.Claims.Should().Contain(c => c.Type == "permissions");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn15Minutes()
    {
        var user = CreateTestUser();

        var tokenString = _tokenService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), precision: TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuerAndAudience()
    {
        var user = CreateTestUser();

        var tokenString = _tokenService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Issuer.Should().Be(PlatformConstants.JwtIssuer);
        token.Audiences.Should().Contain(PlatformConstants.JwtAudience);
    }

    [Fact]
    public void GenerateAccessToken_IsValidSignature()
    {
        var user = CreateTestUser();
        var tokenString = _tokenService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = PlatformConstants.JwtIssuer,
            ValidAudience = PlatformConstants.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey))
        };

        var principal = handler.ValidateToken(tokenString, validationParams, out _);
        principal.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_PlatformAdmin_HasAdminPermissions()
    {
        var user = CreateTestUser(UserRole.PlatformAdmin);
        var tokenString = _tokenService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Claims.Should().Contain(c => c.Type == "permissions" && c.Value == "admin:full");
    }

    [Fact]
    public async Task GenerateRefreshToken_CreatesUniqueToken()
    {
        var userId = Guid.NewGuid();

        var token1 = await _tokenService.GenerateRefreshToken(userId);
        var token2 = await _tokenService.GenerateRefreshToken(userId);

        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public async Task GenerateRefreshToken_ExpiresIn7Days()
    {
        var token = await _tokenService.GenerateRefreshToken(Guid.NewGuid());

        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), precision: TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GenerateRefreshToken_IsSavedToDatabase()
    {
        var userId = Guid.NewGuid();

        var token = await _tokenService.GenerateRefreshToken(userId);

        var savedToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token.Token);
        savedToken.Should().NotBeNull();
        savedToken!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ValidateRefreshToken_ValidToken_ReturnsToken()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser();
        user.Id = userId;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var generated = await _tokenService.GenerateRefreshToken(userId);

        var result = await _tokenService.ValidateRefreshToken(generated.Token);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ValidateRefreshToken_RevokedToken_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var generated = await _tokenService.GenerateRefreshToken(userId);
        await _tokenService.RevokeRefreshToken(generated.Token);

        var result = await _tokenService.ValidateRefreshToken(generated.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshToken_SetsRevokedAt()
    {
        var generated = await _tokenService.GenerateRefreshToken(Guid.NewGuid());

        await _tokenService.RevokeRefreshToken(generated.Token);

        var token = await _db.RefreshTokens.FirstAsync(t => t.Token == generated.Token);
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    private static User CreateTestUser(UserRole role = UserRole.CustomerAdmin)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "Test",
            LastName = "User",
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose() => _db.Dispose();
}
