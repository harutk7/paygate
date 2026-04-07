using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public InvoiceStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime DueDate { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
