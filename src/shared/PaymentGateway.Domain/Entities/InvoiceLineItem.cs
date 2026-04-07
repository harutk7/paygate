namespace PaymentGateway.Domain.Entities;

public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}
