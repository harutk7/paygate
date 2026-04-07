using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;

namespace PaymentGateway.Integration.Tests.Infrastructure;

public class BillingApiFactory : WebApplicationFactory<PaymentGateway.Billing.Api.Data.BillingDbContext>
{
    private readonly string _dbName = $"BillingDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BillingDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Remove hosted services that might interfere with tests
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType == typeof(SubscriptionRenewalJob)).ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);

            // Add in-memory database
            services.AddDbContext<BillingDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PaymentGateway",
                ["Jwt:Audience"] = "PaymentGateway",
                ["Jwt:Key"] = "PaymentGatewayDefaultSecretKey_ChangeInProduction!2024",
                ["ConnectionStrings:paymentdb"] = "unused-for-inmemory"
            });
        });
    }

    public async Task SeedPlans()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // The seed data from model builder doesn't work with InMemory, so seed manually
        if (!await db.Plans.AnyAsync())
        {
            db.Plans.AddRange(
                new PaymentGateway.Domain.Entities.Plan
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "Starter",
                    Tier = PaymentGateway.Domain.Enums.PlanTier.Starter,
                    PriceMonthly = 49m,
                    TransactionLimit = 1000,
                    ApiKeyLimit = 2,
                    RateLimit = 60,
                    Features = "[\"Payment Processing\",\"Basic Analytics\",\"Email Support\"]",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentGateway.Domain.Entities.Plan
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Business",
                    Tier = PaymentGateway.Domain.Enums.PlanTier.Business,
                    PriceMonthly = 199m,
                    TransactionLimit = 10000,
                    ApiKeyLimit = 10,
                    RateLimit = 300,
                    Features = "[\"Payment Processing\",\"Advanced Analytics\",\"Priority Support\",\"Webhooks\",\"Team Management\"]",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentGateway.Domain.Entities.Plan
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Name = "Enterprise",
                    Tier = PaymentGateway.Domain.Enums.PlanTier.Enterprise,
                    PriceMonthly = 799m,
                    TransactionLimit = 100000,
                    ApiKeyLimit = 100,
                    RateLimit = 1000,
                    Features = "[\"Payment Processing\",\"Enterprise Analytics\",\"Dedicated Support\",\"Webhooks\",\"Team Management\",\"Custom Integration\",\"SLA\"]",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
