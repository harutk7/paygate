using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Backoffice.Api.Data;

namespace PaymentGateway.Integration.Tests.Infrastructure;

public class BackofficeApiFactory : WebApplicationFactory<PaymentGateway.Backoffice.Api.Data.BackofficeDbContext>
{
    private readonly string _dbName = $"BackofficeDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<BackofficeDbContext>) ||
                     d.ServiceType == typeof(DbContextOptions<ReadOnlyDbContext>)).ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            // Add in-memory databases
            services.AddDbContext<BackofficeDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
            services.AddDbContext<ReadOnlyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-development-minimum-32-chars!",
                ["ConnectionStrings:paymentdb"] = "unused-for-inmemory"
            });
        });
    }
}
