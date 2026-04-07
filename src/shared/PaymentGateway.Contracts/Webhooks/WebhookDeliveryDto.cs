namespace PaymentGateway.Contracts.Webhooks;

public record WebhookDeliveryDto(
    Guid Id,
    string EventType,
    int? StatusCode,
    int Attempts,
    DateTime CreatedAt,
    DateTime? CompletedAt);
