using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Contracts.Subscriptions;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Services;

public class SubscriptionService
{
    private readonly BillingDbContext _db;
    private readonly IPaymentProcessorService _paymentProcessor;
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        BillingDbContext db,
        IPaymentProcessorService paymentProcessor,
        InvoiceService invoiceService,
        ILogger<SubscriptionService> logger)
    {
        _db = db;
        _paymentProcessor = paymentProcessor;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<SubscriptionDto?> GetCurrentSubscription(Guid orgId)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OrganizationId == orgId && s.Status != SubscriptionStatus.Cancelled)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub == null) return null;

        return MapToDto(sub);
    }

    public async Task<SubscriptionDto> CreateSubscription(Guid orgId, CreateSubscriptionRequest request)
    {
        // Check for existing active subscription
        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

        if (existing != null)
            throw new InvalidOperationException("Organization already has an active subscription. Cancel the current one first.");

        var plan = await _db.Plans.FindAsync(request.PlanId)
            ?? throw new InvalidOperationException("Plan not found.");

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddMonths(1),
            CreatedAt = now,
            UpdatedAt = now
        };

        // Charge via payment processor if payment method is provided
        Payment? payment = null;
        if (request.PaymentMethodId.HasValue)
        {
            var paymentMethod = await _db.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == request.PaymentMethodId.Value && pm.OrganizationId == orgId)
                ?? throw new InvalidOperationException("Payment method not found.");

            // Try to charge
            // In a real implementation, we'd look up the customer profile ID from the org
            var customerProfileId = ""; // Would come from Organization.AuthorizeNetCustomerProfileId
            var result = await _paymentProcessor.ChargeCustomerProfile(
                customerProfileId,
                paymentMethod.AuthorizeNetPaymentProfileId ?? "",
                plan.PriceMonthly,
                $"Subscription: {plan.Name} plan");

            payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                SubscriptionId = subscription.Id,
                Amount = plan.PriceMonthly,
                Currency = "USD",
                Provider = PaymentProvider.AuthorizeNet,
                ProviderTransactionId = result.TransactionId,
                Status = result.Success ? TransactionStatus.Succeeded : TransactionStatus.Failed,
                CreatedAt = now
            };

            if (!result.Success)
            {
                throw new InvalidOperationException($"Payment failed: {result.Message}");
            }
        }
        else
        {
            // Create a pending payment record
            payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                SubscriptionId = subscription.Id,
                Amount = plan.PriceMonthly,
                Currency = "USD",
                Provider = PaymentProvider.AuthorizeNet,
                Status = TransactionStatus.Succeeded,
                CreatedAt = now
            };
        }

        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);

        // Generate invoice
        var invoice = _invoiceService.GenerateInvoice(subscription, payment, plan);
        _db.Invoices.Add(invoice);

        await _db.SaveChangesAsync();

        subscription.Plan = plan;
        return MapToDto(subscription);
    }

    public async Task<SubscriptionDto> CancelSubscription(Guid orgId)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            ?? throw new InvalidOperationException("No active subscription found.");

        sub.CancelledAt = DateTime.UtcNow;
        sub.UpdatedAt = DateTime.UtcNow;
        // Stays active until period end
        await _db.SaveChangesAsync();

        return MapToDto(sub);
    }

    public async Task RenewSubscription(Guid subscriptionId)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId)
            ?? throw new InvalidOperationException("Subscription not found.");

        var defaultPaymentMethod = await _db.PaymentMethods
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pm => pm.OrganizationId == sub.OrganizationId && pm.IsDefault);

        var now = DateTime.UtcNow;
        var plan = sub.Plan;

        if (defaultPaymentMethod != null)
        {
            var customerProfileId = ""; // Would come from Organization
            var result = await _paymentProcessor.ChargeCustomerProfile(
                customerProfileId,
                defaultPaymentMethod.AuthorizeNetPaymentProfileId ?? "",
                plan.PriceMonthly,
                $"Subscription renewal: {plan.Name} plan");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrganizationId = sub.OrganizationId,
                SubscriptionId = sub.Id,
                Amount = plan.PriceMonthly,
                Currency = "USD",
                Provider = PaymentProvider.AuthorizeNet,
                ProviderTransactionId = result.TransactionId,
                Status = result.Success ? TransactionStatus.Succeeded : TransactionStatus.Failed,
                CreatedAt = now
            };

            _db.Payments.Add(payment);

            if (result.Success)
            {
                sub.CurrentPeriodStart = sub.CurrentPeriodEnd;
                sub.CurrentPeriodEnd = sub.CurrentPeriodEnd.AddMonths(1);
                sub.Status = SubscriptionStatus.Active;
                sub.UpdatedAt = now;

                var invoice = _invoiceService.GenerateInvoice(sub, payment, plan);
                _db.Invoices.Add(invoice);
            }
            else
            {
                sub.Status = SubscriptionStatus.PastDue;
                sub.UpdatedAt = now;
                _logger.LogWarning("Subscription {SubscriptionId} renewal failed: {Message}", sub.Id, result.Message);
            }
        }
        else
        {
            sub.Status = SubscriptionStatus.PastDue;
            sub.UpdatedAt = now;
            _logger.LogWarning("Subscription {SubscriptionId} has no default payment method", sub.Id);
        }

        await _db.SaveChangesAsync();
    }

    private static SubscriptionDto MapToDto(Subscription sub) => new(
        sub.Id,
        sub.PlanId,
        sub.Plan.Name,
        sub.Status,
        sub.CurrentPeriodStart,
        sub.CurrentPeriodEnd,
        sub.TrialEnd,
        sub.CancelledAt);
}
