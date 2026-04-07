namespace PaymentGateway.Contracts.Common;

public record ErrorResponse(string Message, Dictionary<string, string[]>? Errors = null);
