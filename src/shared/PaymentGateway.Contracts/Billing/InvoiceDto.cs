using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Billing;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    InvoiceStatus Status,
    DateTime? PaidAt,
    DateTime DueDate,
    DateTime CreatedAt);
