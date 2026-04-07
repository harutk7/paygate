using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;
using PaymentGateway.Contracts.Billing;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly PaymentMethodService _paymentMethodService;
    private readonly InvoiceService _invoiceService;
    private readonly BillingDbContext _db;
    private readonly TenantContext _tenantContext;

    public BillingController(
        PaymentMethodService paymentMethodService,
        InvoiceService invoiceService,
        BillingDbContext db,
        TenantContext tenantContext)
    {
        _paymentMethodService = paymentMethodService;
        _invoiceService = invoiceService;
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpPost("add-payment-method")]
    public async Task<ActionResult<PaymentMethodDto>> AddPaymentMethod([FromBody] AddPaymentMethodRequest request)
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var method = await _paymentMethodService.AddPaymentMethod(orgId, request);
        return Ok(method);
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethods()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var methods = await _paymentMethodService.GetPaymentMethods(orgId);
        return Ok(methods);
    }

    [HttpGet("payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments()
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var payments = await _db.Payments
            .Where(p => p.OrganizationId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.Amount,
                p.Currency,
                p.Status,
                p.ProviderTransactionId,
                p.CreatedAt
            ))
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var orgId = _tenantContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Organization context not found.");

        var allInvoices = await _invoiceService.GetInvoices(orgId);
        var paged = allInvoices.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(allInvoices.Count / (double)pageSize);
        return Ok(new PagedResult<InvoiceDto>(paged, allInvoices.Count, page, pageSize, totalPages));
    }
}
