namespace PaymentGateway.Contracts.Transactions;

public record CreateChargeRequest(
    decimal Amount,
    string Currency,
    string? CardNumber,
    string? Description,
    Dictionary<string, string>? Metadata);
