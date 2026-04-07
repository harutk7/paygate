using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.ApiKeys;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Tests;

public class ApiKeyServiceTests : IDisposable
{
    private readonly GatewayDbContext _db;
    private readonly ApiKeyService _apiKeyService;
    private readonly Guid _orgId = Guid.NewGuid();

    public ApiKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new GatewayDbContext(options);

        var tenant = new TenantContext();
        _apiKeyService = new ApiKeyService(_db, tenant);
    }

    [Fact]
    public async Task CreateApiKey_GeneratesKeyWithCorrectPrefix()
    {
        var request = new CreateApiKeyRequest("Test Key", null);

        var result = await _apiKeyService.CreateApiKey(_orgId, request);

        result.Key.Should().StartWith(PlatformConstants.ApiKeyPrefix);
    }

    [Fact]
    public async Task CreateApiKey_StoredHashMatchesSHA256()
    {
        var request = new CreateApiKeyRequest("Test Key", null);

        var result = await _apiKeyService.CreateApiKey(_orgId, request);

        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(result.Key)));
        var dbKey = await _db.ApiKeys.FirstAsync(k => k.Id == result.Id);
        dbKey.KeyHash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task CreateApiKey_SetsKeyPrefix()
    {
        var request = new CreateApiKeyRequest("Test Key", null);

        var result = await _apiKeyService.CreateApiKey(_orgId, request);

        result.KeyPrefix.Should().Be(result.Key[..12]);
    }

    [Fact]
    public async Task ValidateApiKey_ValidKey_ReturnsSuccess()
    {
        var created = await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Test Key", null));

        var (isValid, orgId, apiKeyId) = await _apiKeyService.ValidateApiKey(created.Key);

        isValid.Should().BeTrue();
        orgId.Should().Be(_orgId);
        apiKeyId.Should().Be(created.Id);
    }

    [Fact]
    public async Task ValidateApiKey_InvalidKey_ReturnsFalse()
    {
        var (isValid, _, _) = await _apiKeyService.ValidateApiKey("pk_live_invalidkey123456789012");

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKey_RevokedKey_ReturnsFalse()
    {
        var created = await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Test Key", null));
        await _apiKeyService.RevokeApiKey(_orgId, created.Id);

        var (isValid, _, _) = await _apiKeyService.ValidateApiKey(created.Key);

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKey_ExpiredKey_ReturnsFalse()
    {
        var created = await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Test Key", DateTime.UtcNow.AddHours(-1)));

        var (isValid, _, _) = await _apiKeyService.ValidateApiKey(created.Key);

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeApiKey_SetsInactiveAndRevokedAt()
    {
        var created = await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Test Key", null));

        await _apiKeyService.RevokeApiKey(_orgId, created.Id);

        var dbKey = await _db.ApiKeys.IgnoreQueryFilters().FirstAsync(k => k.Id == created.Id);
        dbKey.IsActive.Should().BeFalse();
        dbKey.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeApiKey_NonExistentKey_ThrowsKeyNotFound()
    {
        var act = () => _apiKeyService.RevokeApiKey(_orgId, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RotateApiKey_CreatesNewKeyAndSetsGracePeriod()
    {
        var original = await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Test Key", null));

        var rotated = await _apiKeyService.RotateApiKey(_orgId, original.Id);

        rotated.Key.Should().NotBe(original.Key);
        rotated.Name.Should().Be("Test Key");

        var oldKey = await _db.ApiKeys.IgnoreQueryFilters().FirstAsync(k => k.Id == original.Id);
        oldKey.RevokedAt.Should().NotBeNull();
        oldKey.RevokedAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetApiKeys_ReturnsKeysForOrg()
    {
        await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Key 1", null));
        await _apiKeyService.CreateApiKey(_orgId, new CreateApiKeyRequest("Key 2", null));
        await _apiKeyService.CreateApiKey(Guid.NewGuid(), new CreateApiKeyRequest("Other Org Key", null));

        var result = await _apiKeyService.GetApiKeys(_orgId);

        result.Should().HaveCount(2);
    }

    public void Dispose() => _db.Dispose();
}
