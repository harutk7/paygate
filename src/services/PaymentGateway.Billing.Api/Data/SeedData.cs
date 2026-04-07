using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Data;

public static class SeedData
{
    public static readonly Guid StarterPlanId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid BusinessPlanId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid EnterprisePlanId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    public static void Seed(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Plan>().HasData(
            new Plan
            {
                Id = StarterPlanId,
                Name = "Starter",
                Tier = PlanTier.Starter,
                PriceMonthly = 49m,
                TransactionLimit = 1000,
                ApiKeyLimit = 2,
                RateLimit = 60,
                Features = JsonSerializer.Serialize(new[] { "Payment Processing", "Basic Analytics", "Email Support" }),
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Plan
            {
                Id = BusinessPlanId,
                Name = "Business",
                Tier = PlanTier.Business,
                PriceMonthly = 199m,
                TransactionLimit = 10000,
                ApiKeyLimit = 10,
                RateLimit = 300,
                Features = JsonSerializer.Serialize(new[] { "Payment Processing", "Advanced Analytics", "Priority Support", "Webhooks", "Team Management" }),
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Plan
            {
                Id = EnterprisePlanId,
                Name = "Enterprise",
                Tier = PlanTier.Enterprise,
                PriceMonthly = 799m,
                TransactionLimit = 100000,
                ApiKeyLimit = 100,
                RateLimit = 1000,
                Features = JsonSerializer.Serialize(new[] { "Payment Processing", "Enterprise Analytics", "Dedicated Support", "Webhooks", "Team Management", "Custom Integration", "SLA" }),
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}
