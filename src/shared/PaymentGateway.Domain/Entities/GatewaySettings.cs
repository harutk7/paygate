namespace PaymentGateway.Domain.Entities;

public class GatewaySettings : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string? WebhookSecret { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
