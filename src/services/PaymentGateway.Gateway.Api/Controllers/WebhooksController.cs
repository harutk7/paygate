using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Contracts.Webhooks;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly WebhookService _webhookService;
    private readonly TenantContext _tenant;

    public WebhooksController(WebhookService webhookService, TenantContext tenant)
    {
        _webhookService = webhookService;
        _tenant = tenant;
    }

    private Guid OrgId => _tenant.OrganizationId
        ?? throw new UnauthorizedAccessException("Organization not found in claims");

    [HttpGet]
    public async Task<IActionResult> GetWebhooks()
    {
        var result = await _webhookService.GetWebhooks(OrgId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request)
    {
        var result = await _webhookService.CreateWebhook(OrgId, request);
        return Created($"/api/webhooks/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] UpdateWebhookRequest request)
    {
        try
        {
            var result = await _webhookService.UpdateWebhook(OrgId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        try
        {
            await _webhookService.DeleteWebhook(OrgId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id)
    {
        try
        {
            var result = await _webhookService.GetDeliveries(OrgId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestWebhook(Guid id)
    {
        try
        {
            await _webhookService.TestWebhook(OrgId, id);
            return Ok(new { message = "Test webhook queued" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
