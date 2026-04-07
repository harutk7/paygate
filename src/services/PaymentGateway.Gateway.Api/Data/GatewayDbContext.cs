using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Gateway.Api.Data;

public class GatewayDbContext : DbContext
{
    private readonly Guid? _currentOrganizationId;

    public GatewayDbContext(DbContextOptions<GatewayDbContext> options)
        : base(options)
    {
    }

    public GatewayDbContext(DbContextOptions<GatewayDbContext> options, TenantContext tenantContext)
        : base(options)
    {
        _currentOrganizationId = tenantContext.OrganizationId;
    }

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionEvent> TransactionEvents => Set<TransactionEvent>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<GatewaySettings> GatewaySettings => Set<GatewaySettings>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Plan> Plans => Set<Plan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("gateway");

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.ToTable("Organizations", "identity", t => t.ExcludeFromMigrations());
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Subscriptions", "billing", t => t.ExcludeFromMigrations());
            entity.HasOne(e => e.Organization).WithMany(o => o.Subscriptions)
                .HasForeignKey(e => e.OrganizationId);
            entity.HasOne(e => e.Plan).WithMany()
                .HasForeignKey(e => e.PlanId);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Plans", "billing", t => t.ExcludeFromMigrations());
        });

        // Exclude entities discovered via Organization navigation properties
        modelBuilder.Entity<User>().ToTable("Users", "identity", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens", "identity", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.KeyHash).HasMaxLength(512).IsRequired();
            entity.Property(e => e.KeyPrefix).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasOne(e => e.Organization).WithMany(o => o.ApiKeys)
                .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PlatformFee).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.ApiKey).WithMany()
                .HasForeignKey(e => e.ApiKeyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        });

        modelBuilder.Entity<TransactionEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.HasOne(e => e.Transaction).WithMany(t => t.Events)
                .HasForeignKey(e => e.TransactionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebhookEndpoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Secret).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Events).HasMaxLength(2000).IsRequired();
            entity.HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.WebhookEndpoint).WithMany(w => w.Deliveries)
                .HasForeignKey(e => e.WebhookEndpointId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GatewaySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WebhookSecret).HasMaxLength(512);
            entity.HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        });
    }
}
