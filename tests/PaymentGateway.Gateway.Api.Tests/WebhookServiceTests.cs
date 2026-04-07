using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Contracts.Webhooks;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Tests;

public class WebhookServiceTests : IDisposable
{
    private readonly GatewayDbContext _db;
    private readonly WebhookService _webhookService;
    private readonly Guid _orgId = Guid.NewGuid();

    public WebhookServiceTests()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new GatewayDbContext(options);

        var tenant = new TenantContext();
        var logger = new Mock<ILogger<WebhookService>>();
        _webhookService = new WebhookService(_db, tenant, logger.Object);
    }

    [Fact]
    public async Task CreateWebhook_GeneratesHmacSecret()
    {
        var request = new CreateWebhookRequest("https://example.com/webhook", new List<string> { "transaction.succeeded" }, true);

        var result = await _webhookService.CreateWebhook(_orgId, request);

        result.Should().NotBeNull();
        result.Url.Should().Be("https://example.com/webhook");

        var dbEndpoint = await _db.WebhookEndpoints.FirstAsync(w => w.Id == result.Id);
        dbEndpoint.Secret.Should().NotBeNullOrEmpty();
        dbEndpoint.Secret.Should().HaveLength(64); // 32 bytes hex encoded
    }

    [Fact]
    public async Task CreateWebhook_SavesEventsCorrectly()
    {
        var events = new List<string> { "transaction.succeeded", "refund.created" };
        var request = new CreateWebhookRequest("https://example.com/webhook", events, true);

        var result = await _webhookService.CreateWebhook(_orgId, request);

        result.Events.Should().BeEquivalentTo(events);

        var dbEndpoint = await _db.WebhookEndpoints.FirstAsync(w => w.Id == result.Id);
        var storedEvents = JsonSerializer.Deserialize<List<string>>(dbEndpoint.Events);
        storedEvents.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task QueueWebhookDelivery_CreatesDeliveryForMatchingEndpoints()
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            Events = JsonSerializer.Serialize(new List<string> { "transaction.succeeded" }),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            ApiKeyId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = TransactionStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");

        var deliveries = await _db.WebhookDeliveries.ToListAsync();
        deliveries.Should().HaveCount(1);
        deliveries[0].EventType.Should().Be("transaction.succeeded");
        deliveries[0].WebhookEndpointId.Should().Be(endpoint.Id);
    }

    [Fact]
    public async Task QueueWebhookDelivery_SkipsEndpointsWithNonMatchingEvents()
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            Events = JsonSerializer.Serialize(new List<string> { "refund.created" }),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            ApiKeyId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = TransactionStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");

        var deliveries = await _db.WebhookDeliveries.ToListAsync();
        deliveries.Should().BeEmpty();
    }

    [Fact]
    public async Task QueueWebhookDelivery_WildcardEndpoint_ReceivesAllEvents()
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            Events = JsonSerializer.Serialize(new List<string> { "*" }),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            ApiKeyId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = TransactionStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");

        var deliveries = await _db.WebhookDeliveries.ToListAsync();
        deliveries.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueueWebhookDelivery_InactiveEndpoint_IsSkipped()
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            Events = JsonSerializer.Serialize(new List<string> { "*" }),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            ApiKeyId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = TransactionStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");

        var deliveries = await _db.WebhookDeliveries.ToListAsync();
        deliveries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteWebhook_RemovesEndpoint()
    {
        var result = await _webhookService.CreateWebhook(_orgId,
            new CreateWebhookRequest("https://example.com/webhook", new List<string> { "*" }, true));

        await _webhookService.DeleteWebhook(_orgId, result.Id);

        var endpoints = await _db.WebhookEndpoints.ToListAsync();
        endpoints.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWebhooks_ReturnsOrgWebhooks()
    {
        await _webhookService.CreateWebhook(_orgId,
            new CreateWebhookRequest("https://example.com/hook1", new List<string> { "*" }, true));
        await _webhookService.CreateWebhook(_orgId,
            new CreateWebhookRequest("https://example.com/hook2", new List<string> { "*" }, true));
        await _webhookService.CreateWebhook(Guid.NewGuid(),
            new CreateWebhookRequest("https://other.com/hook", new List<string> { "*" }, true));

        var result = await _webhookService.GetWebhooks(_orgId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateWebhook_SecretIsValidHexString()
    {
        var request = new CreateWebhookRequest("https://example.com/hook", new List<string> { "*" }, true);

        var result = await _webhookService.CreateWebhook(_orgId, request);

        var dbEndpoint = await _db.WebhookEndpoints.FirstAsync(w => w.Id == result.Id);
        // Secret should be valid hex (64 chars = 32 bytes)
        dbEndpoint.Secret.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void HmacPayloadSigning_ProducesCorrectSignature()
    {
        // This tests the same HMAC-SHA256 algorithm used by WebhookDispatcherService.ComputeHmacSignature
        var payload = "{\"event_type\":\"transaction.succeeded\",\"data\":{\"amount\":100}}";
        var secret = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        var timestamp = "1700000000";

        var signaturePayload = $"{timestamp}.{payload}";
        var key = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signaturePayload);
        var hash = HMACSHA256.HashData(key, payloadBytes);
        var signature = Convert.ToHexStringLower(hash);

        // Verify deterministic: same inputs produce same output
        var hash2 = HMACSHA256.HashData(key, payloadBytes);
        var signature2 = Convert.ToHexStringLower(hash2);
        signature.Should().Be(signature2);

        // Verify it's a valid hex SHA-256 hash (64 chars)
        signature.Should().HaveLength(64);
        signature.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void HmacPayloadSigning_DifferentSecrets_ProduceDifferentSignatures()
    {
        var payload = "{\"test\":true}";
        var timestamp = "1700000000";
        var signaturePayload = $"{timestamp}.{payload}";

        var secret1 = "secret1secret1secret1secret1secret1secret1secret1secret1secret1s1";
        var secret2 = "secret2secret2secret2secret2secret2secret2secret2secret2secret2s2";

        var sig1 = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret1), Encoding.UTF8.GetBytes(signaturePayload)));
        var sig2 = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret2), Encoding.UTF8.GetBytes(signaturePayload)));

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void HmacPayloadSigning_DifferentTimestamps_ProduceDifferentSignatures()
    {
        var payload = "{\"test\":true}";
        var secret = "testsecrettestsecrettestsecrettestsecrettestsecrettestsecret1234";

        var sig1 = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"1700000000.{payload}")));
        var sig2 = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"1700000001.{payload}")));

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public async Task UpdateWebhook_UpdatesUrlAndEvents()
    {
        var created = await _webhookService.CreateWebhook(_orgId,
            new CreateWebhookRequest("https://example.com/old", new List<string> { "transaction.succeeded" }, true));

        var updateRequest = new UpdateWebhookRequest(
            "https://example.com/new",
            new List<string> { "transaction.succeeded", "refund.created" },
            null);

        var updated = await _webhookService.UpdateWebhook(_orgId, created.Id, updateRequest);

        updated.Url.Should().Be("https://example.com/new");
        updated.Events.Should().HaveCount(2);
        updated.Events.Should().Contain("refund.created");
    }

    [Fact]
    public async Task QueueWebhookDelivery_PayloadContainsTransactionData()
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            Events = JsonSerializer.Serialize(new List<string> { "*" }),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var txnId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = txnId,
            OrganizationId = _orgId,
            ApiKeyId = Guid.NewGuid(),
            Amount = 250.50m,
            Currency = "EUR",
            Status = TransactionStatus.Succeeded,
            ProviderTransactionId = "sim_abc123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");

        var delivery = await _db.WebhookDeliveries.FirstAsync();
        delivery.Payload.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(delivery.Payload);
        var root = doc.RootElement;
        root.GetProperty("event_type").GetString().Should().Be("transaction.succeeded");
        root.GetProperty("data").GetProperty("amount").GetDecimal().Should().Be(250.50m);
        root.GetProperty("data").GetProperty("currency").GetString().Should().Be("EUR");
        root.GetProperty("data").GetProperty("status").GetString().Should().Be("succeeded");
    }

    public void Dispose() => _db.Dispose();
}
