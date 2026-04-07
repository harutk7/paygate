namespace PaymentGateway.Contracts.Webhooks;

public record UpdateWebhookRequest(string? Url, List<string>? Events, bool? IsActive);
