using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.ApiKeys;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Services;

public class ApiKeyService
{
    private readonly GatewayDbContext _db;
    private readonly TenantContext _tenant;

    public ApiKeyService(GatewayDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CreateApiKeyResponse> CreateApiKey(Guid orgId, CreateApiKeyRequest request)
    {
        var rawKey = PlatformConstants.ApiKeyPrefix + GenerateRandomAlphanumeric(32);
        var keyHash = HashKey(rawKey);
        var keyPrefix = rawKey[..12];

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            IsActive = true,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        return new CreateApiKeyResponse(apiKey.Id, apiKey.Name, rawKey, keyPrefix, apiKey.CreatedAt);
    }

    public async Task<(bool IsValid, Guid OrgId, Guid ApiKeyId)> ValidateApiKey(string apiKey)
    {
        var keyHash = HashKey(apiKey);
        var key = await _db.ApiKeys
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (key == null || !key.IsActive)
            return (false, Guid.Empty, Guid.Empty);

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
            return (false, Guid.Empty, Guid.Empty);

        if (key.RevokedAt.HasValue && key.RevokedAt.Value <= DateTime.UtcNow)
            return (false, Guid.Empty, Guid.Empty);

        return (true, key.OrganizationId, key.Id);
    }

    public async Task RevokeApiKey(Guid orgId, Guid keyId)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("API key not found");

        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<CreateApiKeyResponse> RotateApiKey(Guid orgId, Guid keyId)
    {
        var oldKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("API key not found");

        // Mark old key with 24h grace period
        oldKey.RevokedAt = DateTime.UtcNow.AddHours(24);

        var newKeyResponse = await CreateApiKey(orgId, new CreateApiKeyRequest(oldKey.Name, oldKey.ExpiresAt));
        return newKeyResponse;
    }

    public async Task<List<ApiKeyDto>> GetApiKeys(Guid orgId)
    {
        return await _db.ApiKeys
            .Where(k => k.OrganizationId == orgId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto(k.Id, k.Name, k.KeyPrefix, k.IsActive, k.ExpiresAt, k.CreatedAt, k.RevokedAt))
            .ToListAsync();
    }

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexStringLower(bytes);
    }

    private static string GenerateRandomAlphanumeric(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return RandomNumberGenerator.GetString(chars, length);
    }
}
