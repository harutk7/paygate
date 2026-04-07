using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Backoffice.Api.Services;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Tests;

public class AuditLogServiceTests : IDisposable
{
    private readonly BackofficeDbContext _backofficeDb;
    private readonly ReadOnlyDbContext _readDb;
    private readonly AuditLogService _auditLogService;

    public AuditLogServiceTests()
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

        _auditLogService = new AuditLogService(_backofficeDb, _readDb);
    }

    [Fact]
    public async Task LogAction_CreatesEntry()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        await _auditLogService.LogAction(userId, orgId, "UserCreated", "User", userId.ToString(), "New user created", "10.0.0.1");

        var logs = await _backofficeDb.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("UserCreated");
        logs[0].UserId.Should().Be(userId);
        logs[0].OrganizationId.Should().Be(orgId);
        logs[0].EntityType.Should().Be("User");
        logs[0].IpAddress.Should().Be("10.0.0.1");
    }

    [Fact]
    public async Task LogAction_WithNulls_Succeeds()
    {
        await _auditLogService.LogAction(null, null, "SystemEvent", null, null, null, null);

        var logs = await _backofficeDb.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].UserId.Should().BeNull();
        logs[0].OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task GetAuditLog_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 25; i++)
        {
            await _auditLogService.LogAction(null, null, $"Action{i}", null, null, null, null);
        }

        var result = await _auditLogService.GetAuditLog(1, 10, null, null, null, null);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetAuditLog_FiltersByAction()
    {
        await _auditLogService.LogAction(null, null, "UserCreated", null, null, null, null);
        await _auditLogService.LogAction(null, null, "UserDeleted", null, null, null, null);
        await _auditLogService.LogAction(null, null, "UserCreated", null, null, null, null);

        var result = await _auditLogService.GetAuditLog(1, 10, null, null, "UserCreated", null);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditLog_FiltersByDateRange()
    {
        var entry1 = new AuditLog { Id = Guid.NewGuid(), Action = "Old", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var entry2 = new AuditLog { Id = Guid.NewGuid(), Action = "Recent", CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) };
        var entry3 = new AuditLog { Id = Guid.NewGuid(), Action = "New", CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc) };
        _backofficeDb.AuditLogs.AddRange(entry1, entry2, entry3);
        await _backofficeDb.SaveChangesAsync();

        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        var result = await _auditLogService.GetAuditLog(1, 10, from, to, null, null);

        result.Items.Should().HaveCount(1);
        result.Items[0].Action.Should().Be("Recent");
    }

    [Fact]
    public async Task GetAuditLog_FiltersByUserId()
    {
        var userId = Guid.NewGuid();
        await _auditLogService.LogAction(userId, null, "UserAction", null, null, null, null);
        await _auditLogService.LogAction(Guid.NewGuid(), null, "OtherAction", null, null, null, null);

        var result = await _auditLogService.GetAuditLog(1, 10, null, null, null, userId.ToString());

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAuditLog_OrderedByCreatedAtDescending()
    {
        _backofficeDb.AuditLogs.AddRange(
            new AuditLog { Id = Guid.NewGuid(), Action = "First", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new AuditLog { Id = Guid.NewGuid(), Action = "Second", CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new AuditLog { Id = Guid.NewGuid(), Action = "Third", CreatedAt = DateTime.UtcNow });
        await _backofficeDb.SaveChangesAsync();

        var result = await _auditLogService.GetAuditLog(1, 10, null, null, null, null);

        result.Items[0].Action.Should().Be("Third");
        result.Items[2].Action.Should().Be("First");
    }

    public void Dispose()
    {
        _backofficeDb.Dispose();
        _readDb.Dispose();
    }
}
