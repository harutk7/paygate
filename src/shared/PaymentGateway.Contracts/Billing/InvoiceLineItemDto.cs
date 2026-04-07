namespace PaymentGateway.Contracts.Billing;

public record InvoiceLineItemDto(string Description, decimal Amount, int Quantity);
