using PaymentGateway.Contracts.Organizations;
using PaymentGateway.Contracts.Subscriptions;
using PaymentGateway.Contracts.Transactions;

namespace PaymentGateway.Contracts.Admin;

public record CustomerDetailDto(
    OrganizationDto Organization,
    SubscriptionDto? Subscription,
    List<TransactionDto> RecentTransactions,
    int ApiKeyCount,
    decimal TotalVolume);
