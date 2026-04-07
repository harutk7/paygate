namespace PaymentGateway.Contracts.Common;

public record PagedRequest(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDesc = false);
