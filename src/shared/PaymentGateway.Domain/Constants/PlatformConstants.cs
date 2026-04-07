namespace PaymentGateway.Domain.Constants;

public static class PlatformConstants
{
    public const string JwtIssuer = "PaymentGateway";
    public const string JwtAudience = "PaymentGateway";
    public const decimal PlatformFeePercent = 2.9m;
    public const string ApiKeyPrefix = "pk_live_";
}
