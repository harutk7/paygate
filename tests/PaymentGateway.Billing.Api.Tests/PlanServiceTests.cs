using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Tests;

public class PlanServiceTests : IDisposable
{
    private readonly BillingDbContext _db;
    private readonly PlanService _planService;

    public PlanServiceTests()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BillingDbContext(options);
        _planService = new PlanService(_db);
    }

    [Fact]
    public async Task GetAllPlans_ReturnsOnlyActivePlans()
    {
        _db.Plans.AddRange(
            CreatePlan("Starter", PlanTier.Starter, 49m, isActive: true),
            CreatePlan("Business", PlanTier.Business, 199m, isActive: true),
            CreatePlan("Deprecated", PlanTier.Enterprise, 999m, isActive: false));
        await _db.SaveChangesAsync();

        var result = await _planService.GetAllPlans();

        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.Name == "Deprecated");
    }

    [Fact]
    public async Task GetAllPlans_ReturnsPlansOrderedByPrice()
    {
        _db.Plans.AddRange(
            CreatePlan("Enterprise", PlanTier.Enterprise, 799m),
            CreatePlan("Starter", PlanTier.Starter, 49m),
            CreatePlan("Business", PlanTier.Business, 199m));
        await _db.SaveChangesAsync();

        var result = await _planService.GetAllPlans();

        result.Select(p => p.PriceMonthly).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllPlans_DeserializesFeaturesCorrectly()
    {
        var features = new[] { "Payment Processing", "Analytics" };
        var plan = CreatePlan("Starter", PlanTier.Starter, 49m);
        plan.Features = JsonSerializer.Serialize(features);
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();

        var result = await _planService.GetAllPlans();

        result.Should().ContainSingle();
        result[0].Features.Should().BeEquivalentTo(features);
    }

    [Fact]
    public async Task GetAllPlans_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _planService.GetAllPlans();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPlans_MapsDtoFieldsCorrectly()
    {
        var plan = CreatePlan("Business", PlanTier.Business, 199m);
        plan.TransactionLimit = 10000;
        plan.ApiKeyLimit = 10;
        plan.RateLimit = 300;
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();

        var result = await _planService.GetAllPlans();

        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(plan.Id);
        dto.Name.Should().Be("Business");
        dto.Tier.Should().Be(PlanTier.Business);
        dto.PriceMonthly.Should().Be(199m);
        dto.TransactionLimit.Should().Be(10000);
        dto.ApiKeyLimit.Should().Be(10);
        dto.RateLimit.Should().Be(300);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlanById_ExistingPlan_ReturnsPlan()
    {
        var plan = CreatePlan("Starter", PlanTier.Starter, 49m);
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();

        var result = await _planService.GetPlanById(plan.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(plan.Id);
        result.Name.Should().Be("Starter");
    }

    [Fact]
    public async Task GetPlanById_NonExistingPlan_ReturnsNull()
    {
        var result = await _planService.GetPlanById(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlanById_ReturnsInactivePlanToo()
    {
        var plan = CreatePlan("Old Plan", PlanTier.Starter, 29m, isActive: false);
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();

        var result = await _planService.GetPlanById(plan.Id);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    private static Plan CreatePlan(string name, PlanTier tier, decimal price, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Tier = tier,
        PriceMonthly = price,
        TransactionLimit = 1000,
        ApiKeyLimit = 2,
        RateLimit = 60,
        Features = JsonSerializer.Serialize(new[] { "Feature1" }),
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void Dispose() => _db.Dispose();
}
