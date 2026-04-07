namespace PaymentGateway.Domain.Entities;

public class WebhookDelivery : BaseEntity
{
    public Guid WebhookEndpointId { get; set; }
    public WebhookEndpoint WebhookEndpoint { get; set; } = null!;
    public Guid TransactionId { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public int? StatusCode { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
