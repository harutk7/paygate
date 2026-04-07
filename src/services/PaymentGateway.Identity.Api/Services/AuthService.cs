using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Auth;
using PaymentGateway.Contracts.Users;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Identity.Api.Data;

namespace PaymentGateway.Identity.Api.Services;

public partial class AuthService
{
    private readonly IdentityDbContext _db;
    private readonly TokenService _tokenService;

    public AuthService(IdentityDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Register(RegisterRequest request)
    {
        var existingUser = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            throw new InvalidOperationException("A user with this email already exists.");

        var slug = SlugRegex().Replace(request.OrganizationName.ToLowerInvariant(), "-").Trim('-');
        var existingOrg = await _db.Organizations.FirstOrDefaultAsync(o => o.Slug == slug);
        if (existingOrg != null)
            slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.CustomerAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Organizations.Add(organization);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshToken(user.Id);

        return new LoginResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.IsActive, user.CreatedAt));
    }

    public async Task<LoginResponse> Login(LoginRequest request)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshToken(user.Id);

        return new LoginResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.IsActive, user.CreatedAt));
    }

    public async Task<TokenResponse> Refresh(RefreshTokenRequest request)
    {
        var existingToken = await _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (existingToken == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Rotate refresh token
        await _tokenService.RevokeRefreshToken(request.RefreshToken);
        var newRefreshToken = await _tokenService.GenerateRefreshToken(existingToken.UserId);
        var accessToken = _tokenService.GenerateAccessToken(existingToken.User);

        return new TokenResponse(
            accessToken,
            newRefreshToken.Token,
            DateTime.UtcNow.AddMinutes(15));
    }

    public async Task Logout(string refreshToken)
    {
        await _tokenService.RevokeRefreshToken(refreshToken);
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
