namespace PaymentGateway.Contracts.Auth;

public record RegisterRequest(
    string OrganizationName,
    string Email,
    string Password,
    string FirstName,
    string LastName);
