using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Backoffice.Api.Services;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminReportsController : ControllerBase
{
    private readonly RevenueService _revenueService;

    public AdminReportsController(RevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport()
    {
        var report = await _revenueService.GetRevenueReport();
        return Ok(report);
    }
}
