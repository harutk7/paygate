using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Billing.Api.Data;

public class BillingDbContext : DbContext
{
    private readonly Guid? _currentOrganizationId;

    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    public BillingDbContext(DbContextOptions<BillingDbContext> options, TenantContext tenantContext)
        : base(options)
    {
        _currentOrganizationId = tenantContext.OrganizationId;
    }

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("billing");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        // Global query filters for tenant isolation
        modelBuilder.Entity<Subscription>()
            .HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        modelBuilder.Entity<PaymentMethod>()
            .HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);

        // Exclude entities that belong to other schemas (discovered via navigation properties)
        modelBuilder.Entity<Organization>().ToTable("Organizations", "identity", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<User>().ToTable("Users", "identity", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<ApiKey>().ToTable("ApiKeys", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Transaction>().ToTable("Transactions", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<TransactionEvent>().ToTable("TransactionEvents", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<WebhookEndpoint>().ToTable("WebhookEndpoints", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<WebhookDelivery>().ToTable("WebhookDeliveries", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<GatewaySettings>().ToTable("GatewaySettings", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens", "identity", t => t.ExcludeFromMigrations());

        SeedData.Seed(modelBuilder);
    }
}
