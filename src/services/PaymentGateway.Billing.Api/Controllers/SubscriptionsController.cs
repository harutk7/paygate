using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;
using PaymentGateway.Contracts.Subscriptions;

namespace PaymentGateway.Billing.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;
    private readonly TenantContext _tenantContext;

    public SubscriptionsController(SubscriptionService subscriptionService, TenantContext tenantContext)
    {
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionDto>> GetCurrentSubscription()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var sub = await _subscriptionService.GetCurrentSubscription(orgId);
        return Ok(sub);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var sub = await _subscriptionService.CreateSubscription(orgId, request);
        return CreatedAtAction(nameof(GetCurrentSubscription), sub);
    }

    [HttpPut("cancel")]
    public async Task<ActionResult<SubscriptionDto>> CancelSubscription()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var sub = await _subscriptionService.CancelSubscription(orgId);
        return Ok(sub);
    }
}
