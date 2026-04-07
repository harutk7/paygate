using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/charges")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ChargesController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly TenantContext _tenant;

    public ChargesController(TransactionService transactionService, TenantContext tenant)
    {
        _transactionService = transactionService;
        _tenant = tenant;
    }

    private Guid OrgId => _tenant.OrganizationId
        ?? throw new UnauthorizedAccessException("Organization not found in claims");

    private Guid ApiKeyId => _tenant.ApiKeyId
        ?? throw new UnauthorizedAccessException("API key not found in claims");

    [HttpPost]
    public async Task<IActionResult> CreateCharge([FromBody] CreateChargeRequest request)
    {
        try
        {
            var result = await _transactionService.CreateCharge(OrgId, ApiKeyId, request);
            return Created($"/api/v1/charges/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCharge(Guid id)
    {
        try
        {
            var result = await _transactionService.GetTransaction(OrgId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundCharge(Guid id, [FromBody] RefundRequest request)
    {
        try
        {
            var result = await _transactionService.RefundCharge(OrgId, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
