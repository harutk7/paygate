namespace PaymentGateway.Contracts.Admin;

public record DashboardDto(
    int ActiveCustomers,
    decimal MRR,
    decimal TotalVolume,
    int TotalTransactions,
    decimal SuccessRate,
    List<AuditLogDto> RecentActivity);
