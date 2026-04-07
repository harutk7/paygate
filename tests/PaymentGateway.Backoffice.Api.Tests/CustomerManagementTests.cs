using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Backoffice.Api.Services;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Backoffice.Api.Tests;

public class CustomerManagementTests : IDisposable
{
    private readonly BackofficeDbContext _backofficeDb;
    private readonly ReadOnlyDbContext _readDb;
    private readonly CustomerManagementService _customerService;

    public CustomerManagementTests()
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
        _customerService = new CustomerManagementService(_readDb, auditLogService);
    }

    [Fact]
    public async Task GetCustomers_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 15; i++)
        {
            _readDb.Organizations.Add(new Organization
            {
                Id = Guid.NewGuid(), Name = $"Org {i}", Slug = $"org-{i}",
                IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-i), UpdatedAt = DateTime.UtcNow
            });
        }
        await _readDb.SaveChangesAsync();

        var result = await _customerService.GetCustomers(1, 10, null, null);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomers_Page2_ReturnsRemainingItems()
    {
        for (int i = 0; i < 15; i++)
        {
            _readDb.Organizations.Add(new Organization
            {
                Id = Guid.NewGuid(), Name = $"Org {i}", Slug = $"org-{i}",
                IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-i), UpdatedAt = DateTime.UtcNow
            });
        }
        await _readDb.SaveChangesAsync();

        var result = await _customerService.GetCustomers(2, 10, null, null);

        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetCustomers_SearchByName_FiltersCorrectly()
    {
        _readDb.Organizations.AddRange(
            new Organization { Id = Guid.NewGuid(), Name = "Acme Corp", Slug = "acme", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Organization { Id = Guid.NewGuid(), Name = "Beta Inc", Slug = "beta", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _readDb.SaveChangesAsync();

        var result = await _customerService.GetCustomers(1, 10, "acme", null);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetCustomers_FilterByActive_ReturnsOnlyActive()
    {
        _readDb.Organizations.AddRange(
            new Organization { Id = Guid.NewGuid(), Name = "Active", Slug = "active", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Organization { Id = Guid.NewGuid(), Name = "Suspended", Slug = "suspended", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _readDb.SaveChangesAsync();

        var result = await _customerService.GetCustomers(1, 10, null, "active");

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task UpdateCustomerStatus_SuspendsCustomer()
    {
        var orgId = Guid.NewGuid();
        _readDb.Organizations.Add(new Organization
        {
            Id = orgId, Name = "Test Org", Slug = "test-org",
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _readDb.SaveChangesAsync();

        var result = await _customerService.UpdateCustomerStatus(orgId, false, Guid.NewGuid(), "127.0.0.1");

        result.Should().BeTrue();
        var org = await _readDb.Organizations.FindAsync(orgId);
        org!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomerStatus_ActivatesCustomer()
    {
        var orgId = Guid.NewGuid();
        _readDb.Organizations.Add(new Organization
        {
            Id = orgId, Name = "Test Org", Slug = "test-org",
            IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _readDb.SaveChangesAsync();

        var result = await _customerService.UpdateCustomerStatus(orgId, true, Guid.NewGuid(), "127.0.0.1");

        result.Should().BeTrue();
        var org = await _readDb.Organizations.FindAsync(orgId);
        org!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCustomerStatus_NonExistentOrg_ReturnsFalse()
    {
        var result = await _customerService.UpdateCustomerStatus(Guid.NewGuid(), false, null, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomerStatus_CreatesAuditLog()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _readDb.Organizations.Add(new Organization
        {
            Id = orgId, Name = "Test Org", Slug = "test-org",
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await _readDb.SaveChangesAsync();

        await _customerService.UpdateCustomerStatus(orgId, false, userId, "10.0.0.1");

        var auditLogs = await _backofficeDb.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].Action.Should().Be("CustomerSuspended");
        auditLogs[0].UserId.Should().Be(userId);
    }

    public void Dispose()
    {
        _backofficeDb.Dispose();
        _readDb.Dispose();
    }
}
