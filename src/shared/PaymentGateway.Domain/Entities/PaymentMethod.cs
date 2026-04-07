namespace PaymentGateway.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Last4 { get; set; } = null!;
    public string CardBrand { get; set; } = null!;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public string? AuthorizeNetPaymentProfileId { get; set; }
}
