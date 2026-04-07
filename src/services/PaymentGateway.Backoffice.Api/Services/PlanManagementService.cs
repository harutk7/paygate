using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Plans;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Backoffice.Api.Services;

public class PlanManagementService
{
    private readonly ReadOnlyDbContext _readDb;

    public PlanManagementService(ReadOnlyDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task<List<PlanWithStatsDto>> GetPlansWithStats()
    {
        var plans = await _readDb.Plans
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        var subscriberCounts = await _readDb.Subscriptions
            .Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active)
            .GroupBy(s => s.PlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        return plans.Select(p => new PlanWithStatsDto(
            new PlanDto(
                p.Id, p.Name, p.Tier, p.PriceMonthly,
                p.TransactionLimit, p.ApiKeyLimit, p.RateLimit,
                DeserializeFeatures(p.Features), p.IsActive),
            subscriberCounts.GetValueOrDefault(p.Id, 0)))
        .ToList();
    }

    public async Task<PlanDto> CreatePlan(PlanDto dto)
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Tier = dto.Tier,
            PriceMonthly = dto.PriceMonthly,
            TransactionLimit = dto.TransactionLimit,
            ApiKeyLimit = dto.ApiKeyLimit,
            RateLimit = dto.RateLimit,
            Features = JsonSerializer.Serialize(dto.Features),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _readDb.Plans.Add(plan);
        await _readDb.SaveChangesAsync();

        return dto with { Id = plan.Id };
    }

    public async Task<PlanDto?> UpdatePlan(Guid planId, PlanDto dto)
    {
        var plan = await _readDb.Plans.FindAsync(planId);
        if (plan == null) return null;

        plan.Name = dto.Name;
        plan.Tier = dto.Tier;
        plan.PriceMonthly = dto.PriceMonthly;
        plan.TransactionLimit = dto.TransactionLimit;
        plan.ApiKeyLimit = dto.ApiKeyLimit;
        plan.RateLimit = dto.RateLimit;
        plan.Features = JsonSerializer.Serialize(dto.Features);
        plan.IsActive = dto.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _readDb.SaveChangesAsync();

        return new PlanDto(
            plan.Id, plan.Name, plan.Tier, plan.PriceMonthly,
            plan.TransactionLimit, plan.ApiKeyLimit, plan.RateLimit,
            DeserializeFeatures(plan.Features), plan.IsActive);
    }

    private static List<string> DeserializeFeatures(string features)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(features) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

public record PlanWithStatsDto(PlanDto Plan, int SubscriberCount);
