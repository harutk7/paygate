namespace PaymentGateway.Contracts.Transactions;

public record VolumeByDayEntry(string Date, decimal Amount);

public record TransactionStatsDto(
    decimal TotalVolume,
    int TotalCount,
    decimal SuccessRate,
    decimal AverageAmount,
    List<VolumeByDayEntry> VolumeByDay);
