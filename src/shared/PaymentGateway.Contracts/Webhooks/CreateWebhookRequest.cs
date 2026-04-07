namespace PaymentGateway.Contracts.Webhooks;

public record CreateWebhookRequest(string Url, List<string> Events, bool IsActive);
