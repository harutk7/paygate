using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Backoffice.Api.Services;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminAuditLogController : ControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AdminAuditLogController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null)
    {
        var result = await _auditLogService.GetAuditLog(page, pageSize, from, to, action, userId);
        return Ok(result);
    }
}
