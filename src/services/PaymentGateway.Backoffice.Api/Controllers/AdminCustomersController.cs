using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Backoffice.Api.Services;
using PaymentGateway.Contracts.Admin;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminCustomersController : ControllerBase
{
    private readonly CustomerManagementService _customerService;

    public AdminCustomersController(CustomerManagementService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _customerService.GetCustomers(page, pageSize, search, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerDetail(Guid id)
    {
        var result = await _customerService.GetCustomerDetail(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateCustomerStatus(Guid id, [FromBody] UpdateCustomerStatusRequest request)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await _customerService.UpdateCustomerStatus(id, request.IsActive, userId, ipAddress);
        if (!success) return NotFound();
        return NoContent();
    }
}
