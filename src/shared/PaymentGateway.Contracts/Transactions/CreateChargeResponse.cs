using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Transactions;

public record CreateChargeResponse(
    Guid Id,
    TransactionStatus Status,
    string? ProviderTransactionId,
    decimal Amount,
    string Currency,
    DateTime CreatedAt);
