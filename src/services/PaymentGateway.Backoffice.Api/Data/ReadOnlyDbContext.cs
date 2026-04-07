using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Backoffice.Api.Data;

public class ReadOnlyDbContext : DbContext
{
    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "identity");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Organization).WithMany(o => o.Users).HasForeignKey(e => e.OrganizationId);
            entity.Ignore(e => e.RefreshTokens);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations", "identity");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Plans", "billing");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions", "billing");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Organization).WithMany(o => o.Subscriptions).HasForeignKey(e => e.OrganizationId);
            entity.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", "billing");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Subscription).WithMany().HasForeignKey(e => e.SubscriptionId);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys", "gateway");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Organization).WithMany(o => o.ApiKeys).HasForeignKey(e => e.OrganizationId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions", "gateway");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ApiKey).WithMany().HasForeignKey(e => e.ApiKeyId);
            entity.Ignore(e => e.Events);
            entity.Ignore(e => e.WebhookDeliveries);
        });
    }
}
