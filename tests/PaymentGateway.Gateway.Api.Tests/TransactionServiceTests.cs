using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Contracts.Webhooks;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Tests;

public class TransactionServiceTests : IDisposable
{
    private readonly GatewayDbContext _db;
    private readonly TransactionService _sut;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _apiKeyId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new GatewayDbContext(options);

        var tenant = new TenantContext();
        var webhookLogger = new Mock<ILogger<WebhookService>>();
        var webhookService = new WebhookService(_db, tenant, webhookLogger.Object);

        // Empty config so Authorize.net credentials are missing -> simulates success
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = new Mock<ILogger<TransactionService>>();

        _sut = new TransactionService(_db, tenant, webhookService, configuration, logger.Object);

        SeedOrganizationWithSubscription();
    }

    private void SeedOrganizationWithSubscription()
    {
        var plan = new Plan
        {
            Id = _planId,
            Name = "Business",
            Tier = PlanTier.Business,
            PriceMonthly = 49.99m,
            TransactionLimit = 10000,
            ApiKeyLimit = 10,
            RateLimit = 100,
            Features = "[]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Plans.Add(plan);

        var org = new Organization
        {
            Id = _orgId,
            Name = "Test Org",
            Slug = "test-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            PlanId = _planId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow
        };
        _db.Subscriptions.Add(subscription);

        var apiKey = new ApiKey
        {
            Id = _apiKeyId,
            OrganizationId = _orgId,
            Name = "Test API Key",
            KeyHash = "testhash",
            KeyPrefix = "pk_live_test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ApiKeys.Add(apiKey);

        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateCharge_RecordsTransactionInDatabase()
    {
        // Arrange
        var request = new CreateChargeRequest(100.00m, "USD", null, "Test charge", null);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        result.Amount.Should().Be(100.00m);
        result.Currency.Should().Be("USD");

        var txn = await _db.Transactions.FirstAsync(t => t.Id == result.Id);
        txn.OrganizationId.Should().Be(_orgId);
        txn.ApiKeyId.Should().Be(_apiKeyId);
        txn.Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task CreateCharge_CalculatesPlatformFeeAtTwoPointNinePercent()
    {
        // Arrange
        var request = new CreateChargeRequest(100.00m, "USD", null, null, null);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        var txn = await _db.Transactions.FirstAsync(t => t.Id == result.Id);
        var expectedFee = Math.Round(100.00m * PlatformConstants.PlatformFeePercent / 100m, 2);
        expectedFee.Should().Be(2.90m);
        txn.PlatformFee.Should().Be(expectedFee);
    }

    [Fact]
    public async Task CreateCharge_PlatformFeeCalculation_VariousAmounts()
    {
        // Arrange & Act
        var result1 = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(50.00m, "USD", null, null, null));
        var result2 = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(999.99m, "USD", null, null, null));

        // Assert
        var txn1 = await _db.Transactions.FirstAsync(t => t.Id == result1.Id);
        txn1.PlatformFee.Should().Be(Math.Round(50.00m * 2.9m / 100m, 2)); // 1.45

        var txn2 = await _db.Transactions.FirstAsync(t => t.Id == result2.Id);
        txn2.PlatformFee.Should().Be(Math.Round(999.99m * 2.9m / 100m, 2)); // 29.00
    }

    [Fact]
    public async Task CreateCharge_SimulatedSuccess_SetsStatusToSucceeded()
    {
        // Arrange (no Authorize.net creds -> simulates success)
        var request = new CreateChargeRequest(75.00m, "USD", null, null, null);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        result.Status.Should().Be(TransactionStatus.Succeeded);
        result.ProviderTransactionId.Should().NotBeNullOrEmpty();
        result.ProviderTransactionId.Should().StartWith("sim_");
    }

    [Fact]
    public async Task CreateCharge_CreatesTransactionEvents()
    {
        // Arrange
        var request = new CreateChargeRequest(50.00m, "USD", null, null, null);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        var events = await _db.TransactionEvents.Where(e => e.TransactionId == result.Id).ToListAsync();
        events.Should().HaveCountGreaterThanOrEqualTo(2);
        events.Should().Contain(e => e.Message == "Charge initiated" && e.Status == TransactionStatus.Processing);
        events.Should().Contain(e => e.Message == "Charge succeeded" && e.Status == TransactionStatus.Succeeded);
    }

    [Fact]
    public async Task CreateCharge_StoresMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { { "order_id", "12345" }, { "customer", "john" } };
        var request = new CreateChargeRequest(25.00m, "USD", null, null, metadata);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        var txn = await _db.Transactions.FirstAsync(t => t.Id == result.Id);
        txn.Metadata.Should().NotBeNull();
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(txn.Metadata!);
        parsed.Should().ContainKey("order_id").WhoseValue.Should().Be("12345");
    }

    [Fact]
    public async Task CreateCharge_DefaultCurrencyIsUSD()
    {
        // Arrange
        var request = new CreateChargeRequest(10.00m, null!, null, null, null);

        // Act
        var result = await _sut.CreateCharge(_orgId, _apiKeyId, request);

        // Assert
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task CreateCharge_WithoutActiveSubscription_Throws()
    {
        // Arrange
        var otherOrgId = Guid.NewGuid();
        _db.Organizations.Add(new Organization
        {
            Id = otherOrgId, Name = "No Sub Org", Slug = "no-sub", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var request = new CreateChargeRequest(10.00m, "USD", null, null, null);

        // Act & Assert
        var act = () => _sut.CreateCharge(otherOrgId, _apiKeyId, request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active subscription*");
    }

    [Fact]
    public async Task CreateCharge_ExceedingTransactionLimit_Throws()
    {
        // Arrange - set plan limit to 1
        var plan = await _db.Plans.FirstAsync(p => p.Id == _planId);
        plan.TransactionLimit = 1;
        await _db.SaveChangesAsync();

        // Create one transaction to hit the limit
        await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(10.00m, "USD", null, null, null));

        // Act & Assert - second should exceed limit
        var act = () => _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(10.00m, "USD", null, null, null));
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction limit*");
    }

    [Fact]
    public async Task RefundCharge_SucceededTransaction_UpdatesStatusToRefunded()
    {
        // Arrange
        var charge = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(100.00m, "USD", null, null, null));
        charge.Status.Should().Be(TransactionStatus.Succeeded);

        var refundRequest = new RefundRequest(null, "Customer requested");

        // Act
        var result = await _sut.RefundCharge(_orgId, charge.Id, refundRequest);

        // Assert
        result.Status.Should().Be(TransactionStatus.Refunded);
        result.Amount.Should().Be(100.00m);

        var txn = await _db.Transactions.FirstAsync(t => t.Id == charge.Id);
        txn.Status.Should().Be(TransactionStatus.Refunded);
    }

    [Fact]
    public async Task RefundCharge_CreatesRefundEvent()
    {
        // Arrange
        var charge = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(50.00m, "USD", null, null, null));
        var refundRequest = new RefundRequest(null, "Return item");

        // Act
        await _sut.RefundCharge(_orgId, charge.Id, refundRequest);

        // Assert
        var events = await _db.TransactionEvents.Where(e => e.TransactionId == charge.Id).ToListAsync();
        events.Should().Contain(e => e.Status == TransactionStatus.Refunded);
    }

    [Fact]
    public async Task RefundCharge_NonSucceededTransaction_Throws()
    {
        // Arrange - create a transaction and manually set to Failed
        var charge = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(100.00m, "USD", null, null, null));
        var txn = await _db.Transactions.FirstAsync(t => t.Id == charge.Id);
        txn.Status = TransactionStatus.Failed;
        await _db.SaveChangesAsync();

        // Act & Assert
        var act = () => _sut.RefundCharge(_orgId, charge.Id, new RefundRequest(null, null));
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*succeeded*refunded*");
    }

    [Fact]
    public async Task RefundCharge_NonExistentTransaction_ThrowsKeyNotFound()
    {
        // Act & Assert
        var act = () => _sut.RefundCharge(_orgId, Guid.NewGuid(), new RefundRequest(null, null));
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetTransaction_ReturnsDetailWithEvents()
    {
        // Arrange
        var charge = await _sut.CreateCharge(_orgId, _apiKeyId, new CreateChargeRequest(200.00m, "EUR", null, "Detail test", null));

        // Act
        var detail = await _sut.GetTransaction(_orgId, charge.Id);

        // Assert
        detail.Id.Should().Be(charge.Id);
        detail.Amount.Should().Be(200.00m);
        detail.Currency.Should().Be("EUR");
        detail.ApiKeyName.Should().Be("Test API Key");
        detail.Events.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTransaction_NonExistent_ThrowsKeyNotFound()
    {
        // Act & Assert
        var act = () => _sut.GetTransaction(_orgId, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
