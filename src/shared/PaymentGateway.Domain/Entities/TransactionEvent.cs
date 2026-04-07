using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class TransactionEvent : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public TransactionStatus Status { get; set; }
    public string? Message { get; set; }
}
