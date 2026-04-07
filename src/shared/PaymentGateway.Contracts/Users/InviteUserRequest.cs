using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Contracts.Users;

public record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);
