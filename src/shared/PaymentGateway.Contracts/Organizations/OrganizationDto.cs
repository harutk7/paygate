namespace PaymentGateway.Contracts.Organizations;

public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);
