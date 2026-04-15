using System.Text.Json;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Services;

public class TransactionService
{
    private readonly GatewayDbContext _db;
    private readonly TenantContext _tenant;
    private readonly WebhookService _webhookService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        GatewayDbContext db,
        TenantContext tenant,
        WebhookService webhookService,
        IConfiguration configuration,
        ILogger<TransactionService> logger)
    {
        _db = db;
        _tenant = tenant;
        _webhookService = webhookService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CreateChargeResponse> CreateCharge(Guid orgId, Guid apiKeyId, CreateChargeRequest request)
    {
        // Validate org has active subscription
        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OrganizationId == orgId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Organization does not have an active subscription");

        // Check monthly transaction count against plan limit
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyCount = await _db.Transactions
            .IgnoreQueryFilters()
            .CountAsync(t => t.OrganizationId == orgId && t.CreatedAt >= startOfMonth);

        if (monthlyCount >= subscription.Plan.TransactionLimit)
            throw new InvalidOperationException("Monthly transaction limit reached");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ApiKeyId = apiKeyId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Status = TransactionStatus.Processing,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            PlatformFee = Math.Round(request.Amount * PlatformConstants.PlatformFeePercent / 100m, 2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);

        var initialEvent = new TransactionEvent
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            Status = TransactionStatus.Processing,
            Message = "Charge initiated",
            CreatedAt = DateTime.UtcNow
        };
        _db.TransactionEvents.Add(initialEvent);
        await _db.SaveChangesAsync();

        // Process via Authorize.net
        try
        {
            var (success, providerTxnId, errorMessage) = await ProcessWithAuthorizeNet(request, transaction);

            if (success)
            {
                transaction.Status = TransactionStatus.Succeeded;
                transaction.ProviderTransactionId = providerTxnId;
                transaction.UpdatedAt = DateTime.UtcNow;

                _db.TransactionEvents.Add(new TransactionEvent
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transaction.Id,
                    Status = TransactionStatus.Succeeded,
                    Message = "Charge succeeded",
                    CreatedAt = DateTime.UtcNow
                });

                await _webhookService.QueueWebhookDelivery(transaction, "transaction.succeeded");
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.UpdatedAt = DateTime.UtcNow;

                _db.TransactionEvents.Add(new TransactionEvent
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transaction.Id,
                    Status = TransactionStatus.Failed,
                    Message = errorMessage ?? "Charge failed",
                    CreatedAt = DateTime.UtcNow
                });

                await _webhookService.QueueWebhookDelivery(transaction, "transaction.failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing charge with Authorize.net");
            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;

            _db.TransactionEvents.Add(new TransactionEvent
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                Status = TransactionStatus.Failed,
                Message = "Payment processing error",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new CreateChargeResponse(
            transaction.Id,
            transaction.Status,
            transaction.ProviderTransactionId,
            transaction.Amount,
            transaction.Currency,
            transaction.CreatedAt);
    }

    public async Task<TransactionDetailDto> GetTransaction(Guid orgId, Guid transactionId)
    {
        var txn = await _db.Transactions
            .Include(t => t.Events)
            .Include(t => t.ApiKey)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Transaction not found");

        return new TransactionDetailDto(
            txn.Id,
            txn.Amount,
            txn.Currency,
            txn.Status,
            txn.ProviderTransactionId,
            txn.CreatedAt,
            txn.ApiKey.Name,
            txn.Events.OrderBy(e => e.CreatedAt)
                .Select(e => new TransactionEventDto(e.Id, e.Status, e.Message, e.CreatedAt))
                .ToList(),
            txn.Metadata);
    }

    public async Task<PagedResult<TransactionDto>> GetTransactions(
        Guid orgId,
        PagedRequest paging,
        TransactionStatus? status = null,
        string? currency = null)
    {
        var query = _db.Transactions
            .Include(t => t.ApiKey)
            .Where(t => t.OrganizationId == orgId);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(currency))
            query = query.Where(t => t.Currency == currency);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Currency,
                t.Status,
                t.ProviderTransactionId,
                t.CreatedAt,
                t.ApiKey.Name))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)paging.PageSize);
        return new PagedResult<TransactionDto>(items, totalCount, paging.Page, paging.PageSize, totalPages);
    }

    public async Task<TransactionStatsDto> GetTransactionStats(Guid orgId)
    {
        var transactions = _db.Transactions.Where(t => t.OrganizationId == orgId);

        var totalCount = await transactions.CountAsync();
        var totalVolume = totalCount > 0 ? await transactions.SumAsync(t => t.Amount) : 0m;
        var successCount = await transactions.CountAsync(t => t.Status == TransactionStatus.Succeeded);
        var successRate = totalCount > 0 ? Math.Round((decimal)successCount / totalCount * 100, 2) : 0m;
        var averageAmount = totalCount > 0 ? Math.Round(totalVolume / totalCount, 2) : 0m;

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var volumeByDay = await transactions
            .Where(t => t.CreatedAt >= thirtyDaysAgo && t.Status == TransactionStatus.Succeeded)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new VolumeByDayEntry(g.Key.ToString("yyyy-MM-dd"), g.Sum(t => t.Amount)))
            .OrderBy(v => v.Date)
            .ToListAsync();

        return new TransactionStatsDto(totalVolume, totalCount, successRate, averageAmount, volumeByDay);
    }

    public async Task<CreateChargeResponse> RefundCharge(Guid orgId, Guid transactionId, RefundRequest request)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Transaction not found");

        if (transaction.Status != TransactionStatus.Succeeded)
            throw new InvalidOperationException("Only succeeded transactions can be refunded");

        // Process refund via Authorize.net
        try
        {
            var (success, providerTxnId, errorMessage) = await ProcessRefundWithAuthorizeNet(transaction, request);

            if (success)
            {
                transaction.Status = TransactionStatus.Refunded;
                transaction.UpdatedAt = DateTime.UtcNow;

                _db.TransactionEvents.Add(new TransactionEvent
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transaction.Id,
                    Status = TransactionStatus.Refunded,
                    Message = request.Reason ?? "Refund processed",
                    CreatedAt = DateTime.UtcNow
                });

                await _webhookService.QueueWebhookDelivery(transaction, "refund.created");
            }
            else
            {
                _db.TransactionEvents.Add(new TransactionEvent
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transaction.Id,
                    Status = transaction.Status,
                    Message = errorMessage ?? "Refund failed",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund with Authorize.net");
            _db.TransactionEvents.Add(new TransactionEvent
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                Status = transaction.Status,
                Message = "Refund processing error",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new CreateChargeResponse(
            transaction.Id,
            transaction.Status,
            transaction.ProviderTransactionId,
            transaction.Amount,
            transaction.Currency,
            transaction.CreatedAt);
    }

    private Task<(bool Success, string? ProviderTxnId, string? ErrorMessage)> ProcessWithAuthorizeNet(
        CreateChargeRequest request, Transaction transaction)
    {
        var apiLoginId = _configuration["AuthorizeNet:ApiLoginId"];
        var transactionKey = _configuration["AuthorizeNet:TransactionKey"];

        if (string.IsNullOrEmpty(apiLoginId) || string.IsNullOrEmpty(transactionKey) ||
            apiLoginId == "YOUR_SANDBOX_LOGIN_ID" || transactionKey == "YOUR_SANDBOX_TRANSACTION_KEY")
        {
            _logger.LogWarning("Authorize.net credentials not configured, simulating successful charge");
            return Task.FromResult<(bool, string?, string?)>((true, $"sim_{Guid.NewGuid():N}", null));
        }

        var environment = _configuration["AuthorizeNet:Environment"] == "production"
            ? AuthorizeNet.Environment.PRODUCTION
            : AuthorizeNet.Environment.SANDBOX;
        ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = environment;

        var merchantAuthentication = new merchantAuthenticationType
        {
            name = apiLoginId,
            ItemElementName = ItemChoiceType.transactionKey,
            Item = transactionKey
        };

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
            amount = transaction.Amount
        };

        if (!string.IsNullOrEmpty(request.CardNumber))
        {
            transactionRequest.payment = new paymentType
            {
                Item = new creditCardType
                {
                    cardNumber = request.CardNumber,
                    expirationDate = "XXXX"
                }
            };
        }

        var apiRequest = new createTransactionRequest
        {
            merchantAuthentication = merchantAuthentication,
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(apiRequest);
        controller.Execute();
        var response = controller.GetApiResponse();

        if (response?.transactionResponse?.responseCode == "1")
        {
            return Task.FromResult<(bool, string?, string?)>(
                (true, response.transactionResponse.transId, null));
        }

        var errorMessage = response?.transactionResponse?.errors?.FirstOrDefault()?.errorText
            ?? "Transaction declined";
        return Task.FromResult<(bool, string?, string?)>((false, null, errorMessage));
    }

    private Task<(bool Success, string? ProviderTxnId, string? ErrorMessage)> ProcessRefundWithAuthorizeNet(
        Transaction transaction, RefundRequest request)
    {
        var apiLoginId = _configuration["AuthorizeNet:ApiLoginId"];
        var transactionKey = _configuration["AuthorizeNet:TransactionKey"];

        if (string.IsNullOrEmpty(apiLoginId) || string.IsNullOrEmpty(transactionKey) ||
            apiLoginId == "YOUR_SANDBOX_LOGIN_ID" || transactionKey == "YOUR_SANDBOX_TRANSACTION_KEY")
        {
            _logger.LogWarning("Authorize.net credentials not configured, simulating successful refund");
            return Task.FromResult<(bool, string?, string?)>((true, $"sim_ref_{Guid.NewGuid():N}", null));
        }

        var environment = _configuration["AuthorizeNet:Environment"] == "production"
            ? AuthorizeNet.Environment.PRODUCTION
            : AuthorizeNet.Environment.SANDBOX;
        ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = environment;

        var merchantAuthentication = new merchantAuthenticationType
        {
            name = apiLoginId,
            ItemElementName = ItemChoiceType.transactionKey,
            Item = transactionKey
        };

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.refundTransaction.ToString(),
            amount = request.Amount ?? transaction.Amount,
            refTransId = transaction.ProviderTransactionId
        };

        var apiRequest = new createTransactionRequest
        {
            merchantAuthentication = merchantAuthentication,
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(apiRequest);
        controller.Execute();
        var response = controller.GetApiResponse();

        if (response?.transactionResponse?.responseCode == "1")
        {
            return Task.FromResult<(bool, string?, string?)>(
                (true, response.transactionResponse.transId, null));
        }

        var errorMessage = response?.transactionResponse?.errors?.FirstOrDefault()?.errorText
            ?? "Refund declined";
        return Task.FromResult<(bool, string?, string?)>((false, null, errorMessage));
    }
}
