using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Plan : BaseEntity
{
    public string Name { get; set; } = null!;
    public PlanTier Tier { get; set; }
    public decimal PriceMonthly { get; set; }
    public int TransactionLimit { get; set; }
    public int ApiKeyLimit { get; set; }
    public int RateLimit { get; set; }
    public string Features { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
