using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Identity.Api.Data;

namespace PaymentGateway.Integration.Tests.Infrastructure;

public class IdentityApiFactory : WebApplicationFactory<PaymentGateway.Identity.Api.Program>
{
    private readonly string _dbName = $"IdentityDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory database
            services.AddDbContext<IdentityDbContext>(options =>
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
