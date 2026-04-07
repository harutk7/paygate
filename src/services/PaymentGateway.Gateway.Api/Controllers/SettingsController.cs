using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly GatewayDbContext _db;
    private readonly TenantContext _tenant;

    public SettingsController(GatewayDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid OrgId => _tenant.OrganizationId
        ?? throw new UnauthorizedAccessException("Organization not found in claims");

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _db.GatewaySettings
            .FirstOrDefaultAsync(s => s.OrganizationId == OrgId);

        if (settings == null)
            return Ok(new { organizationId = OrgId, webhookSecret = (string?)null });

        return Ok(new
        {
            organizationId = settings.OrganizationId,
            webhookSecret = settings.WebhookSecret,
            updatedAt = settings.UpdatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var settings = await _db.GatewaySettings
            .FirstOrDefaultAsync(s => s.OrganizationId == OrgId);

        if (settings == null)
        {
            settings = new GatewaySettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                WebhookSecret = request.WebhookSecret,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.GatewaySettings.Add(settings);
        }
        else
        {
            settings.WebhookSecret = request.WebhookSecret;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            organizationId = settings.OrganizationId,
            webhookSecret = settings.WebhookSecret,
            updatedAt = settings.UpdatedAt
        });
    }

    public record UpdateSettingsRequest(string? WebhookSecret);
}
