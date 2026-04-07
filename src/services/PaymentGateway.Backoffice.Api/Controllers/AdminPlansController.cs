using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Backoffice.Api.Services;
using PaymentGateway.Contracts.Plans;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/plans")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminPlansController : ControllerBase
{
    private readonly PlanManagementService _planService;

    public AdminPlansController(PlanManagementService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlansWithStats()
    {
        var plans = await _planService.GetPlansWithStats();
        return Ok(plans);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] PlanDto dto)
    {
        var plan = await _planService.CreatePlan(dto);
        return CreatedAtAction(nameof(GetPlansWithStats), plan);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] PlanDto dto)
    {
        var plan = await _planService.UpdatePlan(id, dto);
        if (plan == null) return NotFound();
        return Ok(plan);
    }
}
