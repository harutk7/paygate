using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Transactions;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    TransactionStatus Status,
    string? ProviderTransactionId,
    DateTime CreatedAt,
    string ApiKeyName);
