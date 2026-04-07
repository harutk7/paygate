using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Contracts.Webhooks;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Services;

public class WebhookService
{
    private readonly GatewayDbContext _db;
    private readonly TenantContext _tenant;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(GatewayDbContext db, TenantContext tenant, ILogger<WebhookService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<WebhookEndpointDto> CreateWebhook(Guid orgId, CreateWebhookRequest request)
    {
        var secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Url = request.Url,
            Secret = secret,
            Events = JsonSerializer.Serialize(request.Events),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        return new WebhookEndpointDto(endpoint.Id, endpoint.Url, request.Events, endpoint.IsActive, endpoint.CreatedAt);
    }

    public async Task<WebhookEndpointDto> UpdateWebhook(Guid orgId, Guid webhookId, UpdateWebhookRequest request)
    {
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Webhook endpoint not found");

        if (request.Url != null)
            endpoint.Url = request.Url;

        if (request.Events != null)
            endpoint.Events = JsonSerializer.Serialize(request.Events);

        if (request.IsActive.HasValue)
            endpoint.IsActive = request.IsActive.Value;

        endpoint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var events = JsonSerializer.Deserialize<List<string>>(endpoint.Events) ?? new List<string>();
        return new WebhookEndpointDto(endpoint.Id, endpoint.Url, events, endpoint.IsActive, endpoint.CreatedAt);
    }

    public async Task DeleteWebhook(Guid orgId, Guid webhookId)
    {
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Webhook endpoint not found");

        _db.WebhookEndpoints.Remove(endpoint);
        await _db.SaveChangesAsync();
    }

    public async Task<List<WebhookEndpointDto>> GetWebhooks(Guid orgId)
    {
        return await _db.WebhookEndpoints
            .Where(w => w.OrganizationId == orgId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WebhookEndpointDto(
                w.Id,
                w.Url,
                JsonSerializer.Deserialize<List<string>>(w.Events, (JsonSerializerOptions?)null) ?? new List<string>(),
                w.IsActive,
                w.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<WebhookDeliveryDto>> GetDeliveries(Guid orgId, Guid webhookId)
    {
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Webhook endpoint not found");

        return await _db.WebhookDeliveries
            .Where(d => d.WebhookEndpointId == webhookId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new WebhookDeliveryDto(
                d.Id,
                d.EventType,
                d.StatusCode,
                d.Attempts,
                d.CreatedAt,
                d.CompletedAt))
            .ToListAsync();
    }

    public async Task QueueWebhookDelivery(Transaction transaction, string eventType)
    {
        var endpoints = await _db.WebhookEndpoints
            .IgnoreQueryFilters()
            .Where(w => w.OrganizationId == transaction.OrganizationId && w.IsActive)
            .ToListAsync();

        foreach (var endpoint in endpoints)
        {
            var events = JsonSerializer.Deserialize<List<string>>(endpoint.Events) ?? new List<string>();
            if (!events.Contains(eventType) && !events.Contains("*"))
                continue;

            var payload = JsonSerializer.Serialize(new
            {
                id = Guid.NewGuid().ToString(),
                event_type = eventType,
                created_at = DateTime.UtcNow,
                data = new
                {
                    transaction_id = transaction.Id,
                    amount = transaction.Amount,
                    currency = transaction.Currency,
                    status = transaction.Status.ToString().ToLowerInvariant(),
                    provider_transaction_id = transaction.ProviderTransactionId
                }
            });

            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookEndpointId = endpoint.Id,
                TransactionId = transaction.Id,
                EventType = eventType,
                Payload = payload,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.WebhookDeliveries.Add(delivery);
        }

        await _db.SaveChangesAsync();
    }

    public async Task TestWebhook(Guid orgId, Guid webhookId)
    {
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.OrganizationId == orgId)
            ?? throw new KeyNotFoundException("Webhook endpoint not found");

        var payload = JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid().ToString(),
            event_type = "webhook.test",
            created_at = DateTime.UtcNow,
            data = new { message = "This is a test webhook delivery" }
        });

        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookEndpointId = endpoint.Id,
            TransactionId = Guid.Empty,
            EventType = "webhook.test",
            Payload = payload,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.WebhookDeliveries.Add(delivery);
        await _db.SaveChangesAsync();
    }
}
