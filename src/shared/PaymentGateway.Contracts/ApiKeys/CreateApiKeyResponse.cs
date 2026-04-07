namespace PaymentGateway.Contracts.ApiKeys;

public record CreateApiKeyResponse(
    Guid Id,
    string Name,
    string Key,
    string KeyPrefix,
    DateTime CreatedAt);
