namespace PaymentGateway.Domain.Enums;

public enum TransactionStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4
}
