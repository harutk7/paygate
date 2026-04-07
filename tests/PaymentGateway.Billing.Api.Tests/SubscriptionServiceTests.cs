using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;
using PaymentGateway.Contracts.Subscriptions;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Billing.Api.Tests;

public class SubscriptionServiceTests : IDisposable
{
    private readonly BillingDbContext _db;
    private readonly Mock<IPaymentProcessorService> _paymentProcessorMock;
    private readonly SubscriptionService _subscriptionService;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BillingDbContext(options);

        _paymentProcessorMock = new Mock<IPaymentProcessorService>();
        var invoiceService = new InvoiceService(_db);
        var logger = new Mock<ILogger<SubscriptionService>>();

        _subscriptionService = new SubscriptionService(_db, _paymentProcessorMock.Object, invoiceService, logger.Object);

        SeedData();
    }

    [Fact]
    public async Task CreateSubscription_WithValidPlan_CreatesSubscription()
    {
        var request = new CreateSubscriptionRequest(_planId, null);

        var result = await _subscriptionService.CreateSubscription(_orgId, request);

        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.PlanId.Should().Be(_planId);
        result.PlanName.Should().Be("Starter");
    }

    [Fact]
    public async Task CreateSubscription_CreatesInvoice()
    {
        var request = new CreateSubscriptionRequest(_planId, null);

        await _subscriptionService.CreateSubscription(_orgId, request);

        var invoices = await _db.Invoices.Where(i => i.OrganizationId == _orgId).ToListAsync();
        invoices.Should().HaveCount(1);
        invoices[0].Amount.Should().Be(29m);
    }

    [Fact]
    public async Task CreateSubscription_WithExistingActive_ThrowsInvalidOperation()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));

        var act = () => _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active subscription*");
    }

    [Fact]
    public async Task CreateSubscription_InvalidPlan_ThrowsInvalidOperation()
    {
        var act = () => _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(Guid.NewGuid(), null));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Plan not found*");
    }

    [Fact]
    public async Task CreateSubscription_WithPaymentMethod_ChargesProcessor()
    {
        var pmId = Guid.NewGuid();
        _db.PaymentMethods.Add(new PaymentMethod
        {
            Id = pmId, OrganizationId = _orgId, CardBrand = "Visa",
            Last4 = "4242", ExpiryMonth = 12, ExpiryYear = 2030,
            IsDefault = true, AuthorizeNetPaymentProfileId = "pp_123",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentProcessorMock
            .Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), "pp_123", 29m, It.IsAny<string>()))
            .ReturnsAsync((true, "txn_123", "Success"));

        var request = new CreateSubscriptionRequest(_planId, pmId);
        var result = await _subscriptionService.CreateSubscription(_orgId, request);

        result.Status.Should().Be(SubscriptionStatus.Active);
        _paymentProcessorMock.Verify(p => p.ChargeCustomerProfile(
            It.IsAny<string>(), "pp_123", 29m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelSubscription_SetsCancelledAt()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));

        var result = await _subscriptionService.CancelSubscription(_orgId);

        result.CancelledAt.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Active); // stays active until period end
    }

    [Fact]
    public async Task CancelSubscription_NoActive_ThrowsInvalidOperation()
    {
        var act = () => _subscriptionService.CancelSubscription(_orgId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active subscription*");
    }

    [Fact]
    public async Task GetCurrentSubscription_ReturnsActiveSubscription()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));

        var result = await _subscriptionService.GetCurrentSubscription(_orgId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetCurrentSubscription_NoSubscription_ReturnsNull()
    {
        var result = await _subscriptionService.GetCurrentSubscription(_orgId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RenewSubscription_WithPaymentMethod_ExtendsPeriod()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));
        var sub = await _db.Subscriptions.FirstAsync();
        var originalEnd = sub.CurrentPeriodEnd;

        _db.PaymentMethods.Add(new PaymentMethod
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CardBrand = "Visa",
            Last4 = "4242", ExpiryMonth = 12, ExpiryYear = 2030,
            IsDefault = true, AuthorizeNetPaymentProfileId = "pp_renew",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentProcessorMock
            .Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), "pp_renew", 29m, It.IsAny<string>()))
            .ReturnsAsync((true, "txn_renew", "Approved"));

        await _subscriptionService.RenewSubscription(sub.Id);

        var renewed = await _db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
        renewed.CurrentPeriodStart.Should().Be(originalEnd);
        renewed.CurrentPeriodEnd.Should().Be(originalEnd.AddMonths(1));
        renewed.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task RenewSubscription_ChargeFails_SetsPastDue()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));
        var sub = await _db.Subscriptions.FirstAsync();

        _db.PaymentMethods.Add(new PaymentMethod
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CardBrand = "Visa",
            Last4 = "0000", ExpiryMonth = 1, ExpiryYear = 2025,
            IsDefault = true, AuthorizeNetPaymentProfileId = "pp_fail",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentProcessorMock
            .Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync((false, "", "Declined"));

        await _subscriptionService.RenewSubscription(sub.Id);

        var renewed = await _db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
        renewed.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task RenewSubscription_NoPaymentMethod_SetsPastDue()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));
        var sub = await _db.Subscriptions.FirstAsync();

        await _subscriptionService.RenewSubscription(sub.Id);

        var renewed = await _db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
        renewed.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task RenewSubscription_NonExistingId_Throws()
    {
        var act = () => _subscriptionService.RenewSubscription(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscription not found.");
    }

    [Fact]
    public async Task RenewSubscription_CreatesPaymentRecord()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));
        var sub = await _db.Subscriptions.FirstAsync();
        var paymentCountBefore = await _db.Payments.CountAsync();

        _db.PaymentMethods.Add(new PaymentMethod
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CardBrand = "Visa",
            Last4 = "1234", ExpiryMonth = 6, ExpiryYear = 2028,
            IsDefault = true, AuthorizeNetPaymentProfileId = "pp_pay",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentProcessorMock
            .Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), "pp_pay", 29m, It.IsAny<string>()))
            .ReturnsAsync((true, "txn_pay", "OK"));

        await _subscriptionService.RenewSubscription(sub.Id);

        var paymentCountAfter = await _db.Payments.CountAsync();
        paymentCountAfter.Should().Be(paymentCountBefore + 1);
    }

    [Fact]
    public async Task RenewSubscription_Success_CreatesInvoice()
    {
        await _subscriptionService.CreateSubscription(_orgId, new CreateSubscriptionRequest(_planId, null));
        var sub = await _db.Subscriptions.FirstAsync();
        var invoiceCountBefore = await _db.Invoices.CountAsync();

        _db.PaymentMethods.Add(new PaymentMethod
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CardBrand = "MC",
            Last4 = "5678", ExpiryMonth = 3, ExpiryYear = 2029,
            IsDefault = true, AuthorizeNetPaymentProfileId = "pp_inv",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentProcessorMock
            .Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), "pp_inv", 29m, It.IsAny<string>()))
            .ReturnsAsync((true, "txn_inv", "OK"));

        await _subscriptionService.RenewSubscription(sub.Id);

        var invoiceCountAfter = await _db.Invoices.CountAsync();
        invoiceCountAfter.Should().Be(invoiceCountBefore + 1);
    }

    private void SeedData()
    {
        _db.Plans.Add(new Plan
        {
            Id = _planId, Name = "Starter", Tier = PlanTier.Starter,
            PriceMonthly = 29m, TransactionLimit = 1000, ApiKeyLimit = 2, RateLimit = 100,
            Features = JsonSerializer.Serialize(new List<string> { "API Access" }),
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
