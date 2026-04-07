namespace PaymentGateway.Contracts.Billing;

public record AddPaymentMethodRequest(string OpaqueData, string? CardNumber);
