using Microsoft.EntityFrameworkCore;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Contracts.Billing;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Billing.Api.Services;

public class PaymentMethodService
{
    private readonly BillingDbContext _db;
    private readonly IPaymentProcessorService _paymentProcessor;

    public PaymentMethodService(BillingDbContext db, IPaymentProcessorService paymentProcessor)
    {
        _db = db;
        _paymentProcessor = paymentProcessor;
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethods(Guid orgId)
    {
        var methods = await _db.PaymentMethods
            .Where(pm => pm.OrganizationId == orgId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync();

        return methods.Select(pm => new PaymentMethodDto(
            pm.Id,
            pm.Last4,
            pm.CardBrand,
            pm.ExpiryMonth,
            pm.ExpiryYear,
            pm.IsDefault
        )).ToList();
    }

    public async Task<PaymentMethodDto> AddPaymentMethod(Guid orgId, AddPaymentMethodRequest request)
    {
        // In a full implementation, we would:
        // 1. Look up the org's AuthorizeNetCustomerProfileId
        // 2. Call _paymentProcessor.AddPaymentMethod to create a payment profile
        // 3. Get back card details from the processor

        // For now, parse card info from opaque data or use defaults
        var isFirstMethod = !await _db.PaymentMethods.AnyAsync(pm => pm.OrganizationId == orgId);

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Last4 = request.CardNumber?[^4..] ?? "0000",
            CardBrand = "Visa",
            ExpiryMonth = DateTime.UtcNow.Month,
            ExpiryYear = DateTime.UtcNow.Year + 3,
            IsDefault = isFirstMethod,
            AuthorizeNetPaymentProfileId = null, // Would be set from processor response
            CreatedAt = DateTime.UtcNow
        };

        _db.PaymentMethods.Add(paymentMethod);
        await _db.SaveChangesAsync();

        return new PaymentMethodDto(
            paymentMethod.Id,
            paymentMethod.Last4,
            paymentMethod.CardBrand,
            paymentMethod.ExpiryMonth,
            paymentMethod.ExpiryYear,
            paymentMethod.IsDefault
        );
    }
}
