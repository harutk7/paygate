namespace PaymentGateway.Contracts.Transactions;

public record RefundRequest(decimal? Amount, string? Reason);
