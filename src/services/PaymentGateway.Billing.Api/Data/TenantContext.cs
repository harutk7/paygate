using System.Security.Claims;

namespace PaymentGateway.Billing.Api.Data;

public class TenantContext
{
    public Guid? OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }

    public void SetFromClaimsPrincipal(ClaimsPrincipal principal)
    {
        var orgClaim = principal.FindFirst("org_id")?.Value;
        if (Guid.TryParse(orgClaim, out var orgId))
        {
            OrganizationId = orgId;
        }

        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(subClaim, out var userId))
        {
            UserId = userId;
        }
    }
}
