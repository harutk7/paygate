using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Organizations;
using PaymentGateway.Identity.Api.Data;

namespace PaymentGateway.Identity.Api.Services;

public class OrganizationService
{
    private readonly IdentityDbContext _db;
    private readonly TenantContext _tenantContext;

    public OrganizationService(IdentityDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationDto> GetCurrentOrganization()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization not set.");

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId)
            ?? throw new KeyNotFoundException("Organization not found.");

        return new OrganizationDto(org.Id, org.Name, org.Slug, org.IsActive, org.CreatedAt);
    }

    public async Task<OrganizationDto> UpdateOrganization(UpdateOrganizationRequest request)
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization not set.");

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId)
            ?? throw new KeyNotFoundException("Organization not found.");

        org.Name = request.Name;
        org.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new OrganizationDto(org.Id, org.Name, org.Slug, org.IsActive, org.CreatedAt);
    }
}
