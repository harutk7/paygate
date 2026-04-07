namespace PaymentGateway.Contracts.Webhooks;

public record WebhookEndpointDto(
    Guid Id,
    string Url,
    List<string> Events,
    bool IsActive,
    DateTime CreatedAt);
