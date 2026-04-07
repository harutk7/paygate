using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Subscriptions;

public record SubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialEnd,
    DateTime? CancelledAt);
