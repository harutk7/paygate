namespace PaymentGateway.Contracts.Admin;

public record RevenueReportDto(
    decimal MRR,
    Dictionary<string, decimal> RevenueByPlan,
    decimal FeeRevenue,
    Dictionary<string, decimal> MrrTrend);
