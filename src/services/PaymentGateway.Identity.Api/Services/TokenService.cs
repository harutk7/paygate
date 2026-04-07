using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Identity.Api.Data;

namespace PaymentGateway.Identity.Api.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly IdentityDbContext _db;

    public TokenService(IConfiguration configuration, IdentityDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured")));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("org_id", user.OrganizationId.ToString()),
            new("permissions", GetPermissions(user.Role))
        };

        var token = new JwtSecurityToken(
            issuer: PlatformConstants.JwtIssuer,
            audience: PlatformConstants.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshToken(Guid userId)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        return token;
    }

    public async Task<RefreshToken?> ValidateRefreshToken(string token)
    {
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                rt.ExpiresAt > DateTime.UtcNow &&
                rt.RevokedAt == null);
    }

    public async Task RevokeRefreshToken(string token)
    {
        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static string GetPermissions(Domain.Enums.UserRole role)
    {
        return role switch
        {
            Domain.Enums.UserRole.PlatformAdmin => "admin:full",
            Domain.Enums.UserRole.CustomerAdmin => "org:manage,users:manage,billing:manage,api:manage",
            Domain.Enums.UserRole.CustomerUser => "api:read,transactions:read",
            _ => ""
        };
    }
}
