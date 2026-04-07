using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly TenantContext _tenant;

    public TransactionsController(TransactionService transactionService, TenantContext tenant)
    {
        _transactionService = transactionService;
        _tenant = tenant;
    }

    private Guid OrgId => _tenant.OrganizationId
        ?? throw new UnauthorizedAccessException("Organization not found in claims");

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] string? currency = null)
    {
        var paging = new PagedRequest(page, pageSize);
        var result = await _transactionService.GetTransactions(OrgId, paging, status, currency);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransaction(Guid id)
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

    [HttpGet("stats")]
    public async Task<IActionResult> GetTransactionStats()
    {
        var result = await _transactionService.GetTransactionStats(OrgId);
        return Ok(result);
    }
}
