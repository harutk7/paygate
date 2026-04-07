using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;

namespace PaymentGateway.Billing.Api.Services;

public class AuthorizeNetPaymentService : IPaymentProcessorService
{
    private readonly string _apiLoginId;
    private readonly string _transactionKey;

    public AuthorizeNetPaymentService(IConfiguration configuration)
    {
        _apiLoginId = configuration["AuthorizeNet:ApiLoginId"] ?? throw new InvalidOperationException("AuthorizeNet:ApiLoginId not configured");
        _transactionKey = configuration["AuthorizeNet:TransactionKey"] ?? throw new InvalidOperationException("AuthorizeNet:TransactionKey not configured");

        var isSandbox = configuration.GetValue<bool>("AuthorizeNet:IsSandbox", true);
        ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment =
            isSandbox ? AuthorizeNet.Environment.SANDBOX : AuthorizeNet.Environment.PRODUCTION;
    }

    private merchantAuthenticationType GetMerchantAuth() => new()
    {
        name = _apiLoginId,
        ItemElementName = ItemChoiceType.transactionKey,
        Item = _transactionKey
    };

    public Task<string> CreateCustomerProfile(Guid orgId, string email, string orgName)
    {
        var merchantAuth = GetMerchantAuth();

        var customerProfile = new customerProfileType
        {
            merchantCustomerId = orgId.ToString(),
            email = email,
            description = orgName
        };

        var request = new createCustomerProfileRequest
        {
            merchantAuthentication = merchantAuth,
            profile = customerProfile,
            validationMode = validationModeEnum.none
        };

        var controller = new createCustomerProfileController(request);
        controller.Execute();

        var response = controller.GetApiResponse();
        if (response?.messages?.resultCode == messageTypeEnum.Ok)
        {
            return Task.FromResult(response.customerProfileId);
        }

        var errorMessage = response?.messages?.message?.FirstOrDefault()?.text ?? "Failed to create customer profile";
        throw new InvalidOperationException(errorMessage);
    }

    public Task<string> AddPaymentMethod(string customerProfileId, string opaqueDataValue, string opaqueDataDescriptor)
    {
        var merchantAuth = GetMerchantAuth();

        var opaqueData = new opaqueDataType
        {
            dataDescriptor = opaqueDataDescriptor,
            dataValue = opaqueDataValue
        };

        var paymentType = new paymentType { Item = opaqueData };

        var paymentProfile = new customerPaymentProfileType
        {
            payment = paymentType
        };

        var request = new createCustomerPaymentProfileRequest
        {
            merchantAuthentication = merchantAuth,
            customerProfileId = customerProfileId,
            paymentProfile = paymentProfile,
            validationMode = validationModeEnum.none
        };

        var controller = new createCustomerPaymentProfileController(request);
        controller.Execute();

        var response = controller.GetApiResponse();
        if (response?.messages?.resultCode == messageTypeEnum.Ok)
        {
            return Task.FromResult(response.customerPaymentProfileId);
        }

        var errorMessage = response?.messages?.message?.FirstOrDefault()?.text ?? "Failed to add payment method";
        throw new InvalidOperationException(errorMessage);
    }

    public Task<(bool Success, string TransactionId, string Message)> ChargeCustomerProfile(
        string customerProfileId, string paymentProfileId, decimal amount, string description)
    {
        var merchantAuth = GetMerchantAuth();

        var profileToCharge = new customerProfilePaymentType
        {
            customerProfileId = customerProfileId,
            paymentProfile = new paymentProfile { paymentProfileId = paymentProfileId }
        };

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
            amount = amount,
            profile = profileToCharge,
            order = new orderType { description = description }
        };

        var request = new createTransactionRequest
        {
            merchantAuthentication = merchantAuth,
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(request);
        controller.Execute();

        var response = controller.GetApiResponse();
        if (response?.transactionResponse != null)
        {
            var txnResponse = response.transactionResponse;
            if (txnResponse.responseCode == "1") // Approved
            {
                return Task.FromResult((true, txnResponse.transId, "Transaction approved"));
            }

            var message = txnResponse.errors?.FirstOrDefault()?.errorText ?? "Transaction declined";
            return Task.FromResult((false, txnResponse.transId ?? "", message));
        }

        var errorMessage = response?.messages?.message?.FirstOrDefault()?.text ?? "Transaction failed";
        return Task.FromResult((false, "", errorMessage));
    }

    public Task<(bool Success, string TransactionId, string Message)> RefundTransaction(
        string transactionId, decimal amount)
    {
        var merchantAuth = GetMerchantAuth();

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.refundTransaction.ToString(),
            amount = amount,
            refTransId = transactionId,
            payment = new paymentType
            {
                Item = new creditCardType
                {
                    cardNumber = "XXXX",
                    expirationDate = "XXXX"
                }
            }
        };

        var request = new createTransactionRequest
        {
            merchantAuthentication = merchantAuth,
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(request);
        controller.Execute();

        var response = controller.GetApiResponse();
        if (response?.transactionResponse != null)
        {
            var txnResponse = response.transactionResponse;
            if (txnResponse.responseCode == "1")
            {
                return Task.FromResult((true, txnResponse.transId, "Refund approved"));
            }

            var message = txnResponse.errors?.FirstOrDefault()?.errorText ?? "Refund declined";
            return Task.FromResult((false, txnResponse.transId ?? "", message));
        }

        var errorMessage = response?.messages?.message?.FirstOrDefault()?.text ?? "Refund failed";
        return Task.FromResult((false, "", errorMessage));
    }

    public Task<(string Status, decimal Amount)> GetTransactionDetails(string transactionId)
    {
        var merchantAuth = GetMerchantAuth();

        var request = new getTransactionDetailsRequest
        {
            merchantAuthentication = merchantAuth,
            transId = transactionId
        };

        var controller = new getTransactionDetailsController(request);
        controller.Execute();

        var response = controller.GetApiResponse();
        if (response?.messages?.resultCode == messageTypeEnum.Ok && response.transaction != null)
        {
            var status = response.transaction.transactionStatus;
            var amount = response.transaction.settleAmount;
            return Task.FromResult((status, amount));
        }

        throw new InvalidOperationException("Failed to get transaction details");
    }
}
