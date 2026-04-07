using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Admin;

public record CustomerDto(
    Guid Id,
    string Name,
    string AdminEmail,
    string PlanName,
    SubscriptionStatus SubscriptionStatus,
    int TransactionCount,
    DateTime CreatedAt);
