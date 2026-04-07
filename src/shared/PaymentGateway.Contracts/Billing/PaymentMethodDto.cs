namespace PaymentGateway.Contracts.Billing;

public record PaymentMethodDto(
    Guid Id,
    string Last4,
    string CardBrand,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault);
