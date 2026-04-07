using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Integration.Tests.Infrastructure;

public class GatewayApiFactory : WebApplicationFactory<PaymentGateway.Gateway.Api.Data.GatewayDbContext>
{
    private readonly string _dbName = $"GatewayDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GatewayDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Remove hosted services (WebhookDispatcher)
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);

            // Add in-memory database
            services.AddDbContext<GatewayDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "PaymentGatewayDefaultSecretKey_ChangeInProduction!2024",
                ["ConnectionStrings:paymentdb"] = "unused-for-inmemory"
            });
        });
    }
}
