using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Controllers;

[ApiController]
[Route("api/admin/transactions")]
[Authorize(Roles = "PlatformAdmin")]
public class AdminTransactionsController : ControllerBase
{
    private readonly ReadOnlyDbContext _readDb;

    public AdminTransactionsController(ReadOnlyDbContext readDb)
    {
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _readDb.Transactions.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(t => t.OrganizationId == organizationId.Value);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id, t.Amount, t.Currency, t.Status,
                t.ProviderTransactionId, t.CreatedAt, t.ApiKey.Name))
            .ToListAsync();

        return Ok(new PagedResult<TransactionDto>(items, totalCount, page, pageSize, totalPages));
    }
}
