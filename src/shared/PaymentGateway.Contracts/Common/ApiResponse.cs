namespace PaymentGateway.Contracts.Common;

public record ApiResponse<T>(bool Success, T? Data, string? Error = null);
