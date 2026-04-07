using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Transactions;

public record TransactionDetailDto(
    Guid Id,
    decimal Amount,
    string Currency,
    TransactionStatus Status,
    string? ProviderTransactionId,
    DateTime CreatedAt,
    string ApiKeyName,
    List<TransactionEventDto> Events,
    string? Metadata);
