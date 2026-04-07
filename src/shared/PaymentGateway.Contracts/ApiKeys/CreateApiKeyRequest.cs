namespace PaymentGateway.Contracts.ApiKeys;

public record CreateApiKeyRequest(string Name, DateTime? ExpiresAt);
