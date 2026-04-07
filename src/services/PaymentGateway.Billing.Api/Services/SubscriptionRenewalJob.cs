using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Services;

public class SubscriptionRenewalJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionRenewalJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public SubscriptionRenewalJob(IServiceScopeFactory scopeFactory, ILogger<SubscriptionRenewalJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription renewal job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRenewals(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription renewals");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessRenewals(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();

        var now = DateTime.UtcNow;

        // Find subscriptions that need renewal: period ended, still active, not cancelled
        var expiredSubscriptions = await db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.CurrentPeriodEnd < now &&
                        s.Status == SubscriptionStatus.Active &&
                        s.CancelledAt == null)
            .ToListAsync(stoppingToken);

        foreach (var sub in expiredSubscriptions)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Renewing subscription {SubscriptionId} for org {OrgId}",
                    sub.Id, sub.OrganizationId);
                await subscriptionService.RenewSubscription(sub.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renew subscription {SubscriptionId}", sub.Id);
            }
        }

        // Handle cancelled subscriptions that have reached period end
        var cancelledSubscriptions = await db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.CurrentPeriodEnd < now &&
                        s.Status == SubscriptionStatus.Active &&
                        s.CancelledAt != null)
            .ToListAsync(stoppingToken);

        foreach (var sub in cancelledSubscriptions)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.UpdatedAt = now;
            _logger.LogInformation("Subscription {SubscriptionId} cancelled after period end", sub.Id);
        }

        // Handle past due subscriptions (retry for up to 7 days)
        var pastDueSubscriptions = await db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.PastDue &&
                        s.CurrentPeriodEnd > now.AddDays(-7))
            .ToListAsync(stoppingToken);

        foreach (var sub in pastDueSubscriptions)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Retrying payment for past-due subscription {SubscriptionId}", sub.Id);
                await subscriptionService.RenewSubscription(sub.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry failed for subscription {SubscriptionId}", sub.Id);
            }
        }

        // Suspend subscriptions past due for more than 7 days
        var toSuspend = await db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.PastDue &&
                        s.CurrentPeriodEnd <= now.AddDays(-7))
            .ToListAsync(stoppingToken);

        foreach (var sub in toSuspend)
        {
            sub.Status = SubscriptionStatus.Suspended;
            sub.UpdatedAt = now;
            _logger.LogWarning("Subscription {SubscriptionId} suspended due to non-payment", sub.Id);
        }

        if (expiredSubscriptions.Count > 0 || cancelledSubscriptions.Count > 0 || pastDueSubscriptions.Count > 0 || toSuspend.Count > 0)
        {
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
