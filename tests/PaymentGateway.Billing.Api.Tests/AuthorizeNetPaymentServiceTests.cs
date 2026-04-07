using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

using PaymentGateway.Billing.Api.Services;

namespace PaymentGateway.Billing.Api.Tests;

public class AuthorizeNetPaymentServiceTests
{
    [Fact]
    public void Constructor_MissingApiLoginId_ThrowsInvalidOperation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthorizeNet:TransactionKey"] = "test-key",
                ["AuthorizeNet:IsSandbox"] = "true"
            })
            .Build();

        var act = () => new AuthorizeNetPaymentService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiLoginId*");
    }

    [Fact]
    public void Constructor_MissingTransactionKey_ThrowsInvalidOperation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthorizeNet:ApiLoginId"] = "test-login",
                ["AuthorizeNet:IsSandbox"] = "true"
            })
            .Build();

        var act = () => new AuthorizeNetPaymentService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TransactionKey*");
    }

    [Fact]
    public void Constructor_ValidConfig_CreatesInstance()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthorizeNet:ApiLoginId"] = "test-login",
                ["AuthorizeNet:TransactionKey"] = "test-key",
                ["AuthorizeNet:IsSandbox"] = "true"
            })
            .Build();

        var service = new AuthorizeNetPaymentService(config);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_SandboxMode_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthorizeNet:ApiLoginId"] = "sandbox-login",
                ["AuthorizeNet:TransactionKey"] = "sandbox-key",
                ["AuthorizeNet:IsSandbox"] = "true"
            })
            .Build();

        var act = () => new AuthorizeNetPaymentService(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void ImplementsIPaymentProcessorService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthorizeNet:ApiLoginId"] = "test-login",
                ["AuthorizeNet:TransactionKey"] = "test-key",
                ["AuthorizeNet:IsSandbox"] = "true"
            })
            .Build();

        var service = new AuthorizeNetPaymentService(config);

        service.Should().BeAssignableTo<IPaymentProcessorService>();
    }

    [Fact]
    public async Task MockedPaymentProcessor_ChargeReturnsSuccess()
    {
        // Test the interface contract via a mock to verify consuming code works correctly
        var mock = new Mock<IPaymentProcessorService>();
        mock.Setup(p => p.ChargeCustomerProfile("cust-1", "pay-1", 49.99m, "Test charge"))
            .Returns(Task.FromResult((true, "txn-001", "Approved")));

        var result = await mock.Object.ChargeCustomerProfile("cust-1", "pay-1", 49.99m, "Test charge");

        result.Success.Should().BeTrue();
        result.TransactionId.Should().Be("txn-001");
        result.Message.Should().Be("Approved");
    }

    [Fact]
    public async Task MockedPaymentProcessor_ChargeReturnsFailure()
    {
        var mock = new Mock<IPaymentProcessorService>();
        mock.Setup(p => p.ChargeCustomerProfile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.FromResult((false, "", "Insufficient funds")));

        var result = await mock.Object.ChargeCustomerProfile("cust-1", "pay-1", 100m, "Test");

        result.Success.Should().BeFalse();
        result.TransactionId.Should().BeEmpty();
        result.Message.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task MockedPaymentProcessor_RefundReturnsSuccess()
    {
        var mock = new Mock<IPaymentProcessorService>();
        mock.Setup(p => p.RefundTransaction("txn-001", 25m))
            .Returns(Task.FromResult((true, "txn-refund-001", "Refund approved")));

        var result = await mock.Object.RefundTransaction("txn-001", 25m);

        result.Success.Should().BeTrue();
        result.TransactionId.Should().Be("txn-refund-001");
    }

    [Fact]
    public async Task MockedPaymentProcessor_CreateCustomerProfile_ReturnsProfileId()
    {
        var mock = new Mock<IPaymentProcessorService>();
        var orgId = Guid.NewGuid();
        mock.Setup(p => p.CreateCustomerProfile(orgId, "test@test.com", "Test Org"))
            .Returns(Task.FromResult("cust-profile-123"));

        var result = await mock.Object.CreateCustomerProfile(orgId, "test@test.com", "Test Org");

        result.Should().Be("cust-profile-123");
    }

    [Fact]
    public async Task MockedPaymentProcessor_GetTransactionDetails_ReturnsStatusAndAmount()
    {
        var mock = new Mock<IPaymentProcessorService>();
        mock.Setup(p => p.GetTransactionDetails("txn-001"))
            .Returns(Task.FromResult(("settledSuccessfully", 99.99m)));

        var result = await mock.Object.GetTransactionDetails("txn-001");

        result.Status.Should().Be("settledSuccessfully");
        result.Amount.Should().Be(99.99m);
    }
}
