using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Identity.Api.Data;

public class IdentityDbContext : DbContext
{
    private readonly Guid? _currentOrganizationId;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, TenantContext tenantContext)
        : base(options)
    {
        _currentOrganizationId = tenantContext.OrganizationId;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => _currentOrganizationId == null || e.OrganizationId == _currentOrganizationId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Exclude entities that belong to other schemas (discovered via navigation properties)
        modelBuilder.Entity<ApiKey>().ToTable("ApiKeys", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Subscription>().ToTable("Subscriptions", "billing", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Plan>().ToTable("Plans", "billing", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Transaction>().ToTable("Transactions", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<TransactionEvent>().ToTable("TransactionEvents", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<WebhookEndpoint>().ToTable("WebhookEndpoints", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<WebhookDelivery>().ToTable("WebhookDeliveries", "gateway", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<GatewaySettings>().ToTable("GatewaySettings", "gateway", t => t.ExcludeFromMigrations());

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var adminOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        modelBuilder.Entity<Organization>().HasData(new Organization
        {
            Id = adminOrgId,
            Name = "Platform Administration",
            Slug = "platform-admin",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            OrganizationId = adminOrgId,
            Email = "admin@paygate.local",
            PasswordHash = "$2a$11$dprj7P4sF9a.2qA8EGsbhupcuKCwAOxgQbqFF7.TbbC1wLZZ/2Hie", // BCrypt hash of "Admin123!"
            FirstName = "Platform",
            LastName = "Admin",
            Role = Domain.Enums.UserRole.PlatformAdmin,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
