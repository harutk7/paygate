using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Contracts.Plans;

namespace PaymentGateway.Billing.Api.Services;

public class PlanService
{
    private readonly BillingDbContext _db;

    public PlanService(BillingDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlanDto>> GetAllPlans()
    {
        var plans = await _db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        return plans.Select(p => new PlanDto(
            p.Id,
            p.Name,
            p.Tier,
            p.PriceMonthly,
            p.TransactionLimit,
            p.ApiKeyLimit,
            p.RateLimit,
            JsonSerializer.Deserialize<List<string>>(p.Features) ?? [],
            p.IsActive
        )).ToList();
    }

    public async Task<PlanDto?> GetPlanById(Guid id)
    {
        var p = await _db.Plans.FindAsync(id);
        if (p == null) return null;

        return new PlanDto(
            p.Id,
            p.Name,
            p.Tier,
            p.PriceMonthly,
            p.TransactionLimit,
            p.ApiKeyLimit,
            p.RateLimit,
            JsonSerializer.Deserialize<List<string>>(p.Features) ?? [],
            p.IsActive
        );
    }
}
