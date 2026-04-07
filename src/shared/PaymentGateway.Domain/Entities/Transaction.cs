using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public TransactionStatus Status { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? Metadata { get; set; }
    public decimal PlatformFee { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TransactionEvent> Events { get; set; } = new List<TransactionEvent>();
    public ICollection<WebhookDelivery> WebhookDeliveries { get; set; } = new List<WebhookDelivery>();
}
