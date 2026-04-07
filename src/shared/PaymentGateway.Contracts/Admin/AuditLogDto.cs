namespace PaymentGateway.Contracts.Admin;

public record AuditLogDto(
    Guid Id,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Details,
    DateTime CreatedAt);
