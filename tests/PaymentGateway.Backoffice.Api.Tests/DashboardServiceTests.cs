using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Backoffice.Api.Services;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly BackofficeDbContext _backofficeDb;
    private readonly ReadOnlyDbContext _readDb;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var backofficeOptions = new DbContextOptionsBuilder<BackofficeDbContext>()
            .UseInMemoryDatabase(dbName + "_backoffice")
            .Options;
        _backofficeDb = new BackofficeDbContext(backofficeOptions);

        var readOptions = new DbContextOptionsBuilder<ReadOnlyDbContext>()
            .UseInMemoryDatabase(dbName + "_readonly")
            .Options;
        _readDb = new ReadOnlyDbContext(readOptions);

        var auditLogService = new AuditLogService(_backofficeDb, _readDb);
        _dashboardService = new DashboardService(_readDb, auditLogService);
    }

    [Fact]
    public async Task GetDashboard_CountsActiveCustomers()
    {
        _readDb.Organizations.AddRange(
            new Organization { Id = Guid.NewGuid(), Name = "Active 1", Slug = "a1", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Organization { Id = Guid.NewGuid(), Name = "Active 2", Slug = "a2", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Organization { Id = Guid.NewGuid(), Name = "Inactive", Slug = "i1", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _readDb.SaveChangesAsync();

        var result = await _dashboardService.GetDashboard();

        result.ActiveCustomers.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboard_CalculatesMrr()
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org", Slug = "org", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var plan = new Plan
        {
            Id = Guid.NewGuid(), Name = "Pro", Tier = PlanTier.Business,
            PriceMonthly = 99m, TransactionLimit = 5000, ApiKeyLimit = 5, RateLimit = 200,
            Features = "[]", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _readDb.Organizations.Add(org);
        _readDb.Plans.Add(plan);
        _readDb.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _readDb.SaveChangesAsync();

        var result = await _dashboardService.GetDashboard();

        result.MRR.Should().Be(99m);
    }

    [Fact]
    public async Task GetDashboard_CalculatesTransactionStats()
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org", Slug = "org", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, Name = "Key",
            KeyHash = "hash", KeyPrefix = "pk_live_", IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _readDb.Organizations.Add(org);
        _readDb.ApiKeys.Add(apiKey);
        _readDb.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), OrganizationId = org.Id, ApiKeyId = apiKey.Id, Amount = 100m, Currency = "USD", Status = TransactionStatus.Succeeded, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Transaction { Id = Guid.NewGuid(), OrganizationId = org.Id, ApiKeyId = apiKey.Id, Amount = 50m, Currency = "USD", Status = TransactionStatus.Succeeded, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Transaction { Id = Guid.NewGuid(), OrganizationId = org.Id, ApiKeyId = apiKey.Id, Amount = 25m, Currency = "USD", Status = TransactionStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _readDb.SaveChangesAsync();

        var result = await _dashboardService.GetDashboard();

        result.TotalVolume.Should().Be(150m);
        result.TotalTransactions.Should().Be(3);
        result.SuccessRate.Should().Be(66.67m);
    }

    [Fact]
    public async Task GetDashboard_EmptyData_ReturnsZeros()
    {
        var result = await _dashboardService.GetDashboard();

        result.ActiveCustomers.Should().Be(0);
        result.MRR.Should().Be(0);
        result.TotalVolume.Should().Be(0);
        result.TotalTransactions.Should().Be(0);
        result.SuccessRate.Should().Be(0);
    }

    public void Dispose()
    {
        _backofficeDb.Dispose();
        _readDb.Dispose();
    }
}
