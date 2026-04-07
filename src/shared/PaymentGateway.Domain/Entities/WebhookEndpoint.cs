namespace PaymentGateway.Domain.Entities;

public class WebhookEndpoint : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Url { get; set; } = null!;
    public string Secret { get; set; } = null!;
    public string Events { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
