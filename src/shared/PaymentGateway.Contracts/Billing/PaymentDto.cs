using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Billing;

public record PaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    TransactionStatus Status,
    string? ProviderTransactionId,
    DateTime CreatedAt);
