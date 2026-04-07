using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Services;

public class WebhookDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDispatcherService> _logger;

    private static readonly int[] RetryDelaysMinutes = [1, 5, 30, 120, 1440];

    public WebhookDispatcherService(IServiceScopeFactory scopeFactory, ILogger<WebhookDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveries(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook deliveries");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessPendingDeliveries(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();

        var now = DateTime.UtcNow;
        var deliveries = await db.WebhookDeliveries
            .Include(d => d.WebhookEndpoint)
            .Where(d => d.CompletedAt == null &&
                        (d.NextRetryAt == null || d.NextRetryAt <= now))
            .Take(50)
            .ToListAsync(ct);

        if (deliveries.Count == 0)
            return;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        foreach (var delivery in deliveries)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var signature = ComputeHmacSignature(delivery.Payload, delivery.WebhookEndpoint.Secret, timestamp);

                var request = new HttpRequestMessage(HttpMethod.Post, delivery.WebhookEndpoint.Url)
                {
                    Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Id", delivery.Id.ToString());
                request.Headers.Add("X-Webhook-Timestamp", timestamp);

                var response = await httpClient.SendAsync(request, ct);
                delivery.StatusCode = (int)response.StatusCode;
                delivery.Attempts++;

                if (response.IsSuccessStatusCode)
                {
                    delivery.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    HandleFailedAttempt(delivery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery {DeliveryId} failed", delivery.Id);
                delivery.Attempts++;
                delivery.StatusCode = 0;
                HandleFailedAttempt(delivery);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void HandleFailedAttempt(Domain.Entities.WebhookDelivery delivery)
    {
        if (delivery.Attempts >= 5)
        {
            delivery.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            var delayMinutes = delivery.Attempts <= RetryDelaysMinutes.Length
                ? RetryDelaysMinutes[delivery.Attempts - 1]
                : RetryDelaysMinutes[^1];
            delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
    }

    private static string ComputeHmacSignature(string payload, string secret, string timestamp)
    {
        var signaturePayload = $"{timestamp}.{payload}";
        var key = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signaturePayload);
        var hash = HMACSHA256.HashData(key, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }
}
