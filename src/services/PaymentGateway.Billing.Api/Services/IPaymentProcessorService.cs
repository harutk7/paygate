namespace PaymentGateway.Billing.Api.Services;

public interface IPaymentProcessorService
{
    Task<string> CreateCustomerProfile(Guid orgId, string email, string orgName);
    Task<string> AddPaymentMethod(string customerProfileId, string opaqueDataValue, string opaqueDataDescriptor);
    Task<(bool Success, string TransactionId, string Message)> ChargeCustomerProfile(string customerProfileId, string paymentProfileId, decimal amount, string description);
    Task<(bool Success, string TransactionId, string Message)> RefundTransaction(string transactionId, decimal amount);
    Task<(string Status, decimal Amount)> GetTransactionDetails(string transactionId);
}
