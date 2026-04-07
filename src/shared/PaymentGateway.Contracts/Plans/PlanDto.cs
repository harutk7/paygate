using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Plans;

public record PlanDto(
    Guid Id,
    string Name,
    PlanTier Tier,
    decimal PriceMonthly,
    int TransactionLimit,
    int ApiKeyLimit,
    int RateLimit,
    List<string> Features,
    bool IsActive);
