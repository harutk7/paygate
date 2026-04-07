using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Users;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
