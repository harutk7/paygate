using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Admin;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Services;

public class RevenueService
{
    private readonly ReadOnlyDbContext _readDb;

    public RevenueService(ReadOnlyDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task<RevenueReportDto> GetRevenueReport()
    {
        var mrr = await _readDb.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Join(_readDb.Plans, s => s.PlanId, p => p.Id, (s, p) => p.PriceMonthly)
            .SumAsync(price => price);

        var revenueByPlan = await _readDb.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Join(_readDb.Plans, s => s.PlanId, p => p.Id, (s, p) => new { p.Name, p.PriceMonthly })
            .GroupBy(x => x.Name)
            .Select(g => new { PlanName = g.Key, Total = g.Sum(x => x.PriceMonthly) })
            .ToDictionaryAsync(x => x.PlanName, x => x.Total);

        var feeRevenue = await _readDb.Transactions
            .Where(t => t.Status == TransactionStatus.Succeeded)
            .SumAsync(t => t.PlatformFee);

        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var mrrTrend = await _readDb.Payments
            .Where(p => p.CreatedAt >= twelveMonthsAgo && p.Status == TransactionStatus.Succeeded)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                Key = g.Key.Year + "-" + g.Key.Month.ToString().PadLeft(2, '0'),
                Total = g.Sum(p => p.Amount)
            })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

        return new RevenueReportDto(mrr, revenueByPlan, feeRevenue, mrrTrend);
    }
}
