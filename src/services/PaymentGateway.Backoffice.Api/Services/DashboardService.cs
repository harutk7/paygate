using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Admin;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Services;

public class DashboardService
{
    private readonly ReadOnlyDbContext _readDb;
    private readonly AuditLogService _auditLogService;

    public DashboardService(ReadOnlyDbContext readDb, AuditLogService auditLogService)
    {
        _readDb = readDb;
        _auditLogService = auditLogService;
    }

    public async Task<DashboardDto> GetDashboard()
    {
        var activeCustomers = await _readDb.Organizations
            .Where(o => o.IsActive)
            .CountAsync();

        var mrr = await _readDb.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Join(_readDb.Plans, s => s.PlanId, p => p.Id, (s, p) => p.PriceMonthly)
            .SumAsync(price => price);

        var totalVolume = await _readDb.Transactions
            .Where(t => t.Status == TransactionStatus.Succeeded)
            .SumAsync(t => t.Amount);

        var totalTransactions = await _readDb.Transactions.CountAsync();

        var succeededCount = await _readDb.Transactions
            .Where(t => t.Status == TransactionStatus.Succeeded)
            .CountAsync();

        var successRate = totalTransactions > 0
            ? Math.Round((decimal)succeededCount / totalTransactions * 100, 2)
            : 0m;

        var auditLogResult = await _auditLogService.GetAuditLog(1, 20, null, null, null, null);

        return new DashboardDto(
            activeCustomers,
            mrr,
            totalVolume,
            totalTransactions,
            successRate,
            auditLogResult.Items);
    }
}
