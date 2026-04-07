using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Backoffice.Api.Services;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminDashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public AdminDashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _dashboardService.GetDashboard();
        return Ok(dashboard);
    }
}
