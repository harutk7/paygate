using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public TransactionStatus Status { get; set; }
}
