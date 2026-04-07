using PaymentGateway.Contracts.Users;

namespace PaymentGateway.Contracts.Auth;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
