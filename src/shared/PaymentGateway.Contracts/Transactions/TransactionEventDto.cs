using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Transactions;

public record TransactionEventDto(
    Guid Id,
    TransactionStatus Status,
    string? Message,
    DateTime CreatedAt);
