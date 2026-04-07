using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Contracts.Billing;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Services;

public class InvoiceService
{
    private readonly BillingDbContext _db;

    public InvoiceService(BillingDbContext db)
    {
        _db = db;
    }

    public async Task<List<InvoiceDto>> GetInvoices(Guid orgId)
    {
        var invoices = await _db.Invoices
            .Where(i => i.OrganizationId == orgId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(i => new InvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.Amount,
            i.Currency,
            i.Status,
            i.PaidAt,
            i.DueDate,
            i.CreatedAt
        )).ToList();
    }

    public Invoice GenerateInvoice(Subscription subscription, Payment payment, Plan plan)
    {
        var now = DateTime.UtcNow;
        var invoiceNumber = $"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = subscription.OrganizationId,
            SubscriptionId = subscription.Id,
            InvoiceNumber = invoiceNumber,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status == TransactionStatus.Succeeded ? InvoiceStatus.Paid : InvoiceStatus.Issued,
            PaidAt = payment.Status == TransactionStatus.Succeeded ? now : null,
            DueDate = now.AddDays(30),
            CreatedAt = now,
            LineItems = new List<InvoiceLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = $"{plan.Name} Plan - Monthly Subscription",
                    Amount = plan.PriceMonthly,
                    Quantity = 1,
                    CreatedAt = now
                }
            }
        };

        return invoice;
    }
}
