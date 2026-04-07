namespace PaymentGateway.Contracts.ApiKeys;

public record ApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime? RevokedAt);
