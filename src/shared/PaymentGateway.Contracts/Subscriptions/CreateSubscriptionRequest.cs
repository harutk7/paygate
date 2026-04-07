namespace PaymentGateway.Contracts.Subscriptions;

public record CreateSubscriptionRequest(Guid PlanId, Guid? PaymentMethodId);
