using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Billing.Api.Services;
using PaymentGateway.Contracts.Plans;

namespace PaymentGateway.Billing.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly PlanService _planService;

    public PlansController(PlanService planService)
    {
        _planService = planService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<PlanDto>>> GetPlans()
    {
        var plans = await _planService.GetAllPlans();
        return Ok(plans);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanDto>> GetPlan(Guid id)
    {
        var plan = await _planService.GetPlanById(id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }
}
