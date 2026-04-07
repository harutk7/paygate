using System.Collections.Concurrent;
using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Services;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<Guid, SlidingWindowCounter> Counters = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, GatewayDbContext db)
    {
        // Only rate limit /api/v1/ routes
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        if (tenantContext.OrganizationId is not { } orgId)
        {
            await _next(context);
            return;
        }

        // Look up org's plan rate limit
        var subscription = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                db.Subscriptions
                    .Where(s => s.OrganizationId == orgId &&
                                s.Status == Domain.Enums.SubscriptionStatus.Active),
                context.RequestAborted);

        var rateLimit = 60; // default
        if (subscription != null)
        {
            var plan = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(
                    db.Plans.Where(p => p.Id == subscription.PlanId),
                    context.RequestAborted);
            if (plan != null)
                rateLimit = plan.RateLimit;
        }

        var counter = Counters.GetOrAdd(orgId, _ => new SlidingWindowCounter());
        if (!counter.TryIncrement(rateLimit))
        {
            _logger.LogWarning("Rate limit exceeded for org {OrgId}", orgId);
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = "60";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    private class SlidingWindowCounter
    {
        private readonly object _lock = new();
        private readonly Queue<DateTime> _timestamps = new();

        public bool TryIncrement(int limit)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-1);

                while (_timestamps.Count > 0 && _timestamps.Peek() < windowStart)
                    _timestamps.Dequeue();

                if (_timestamps.Count >= limit)
                    return false;

                _timestamps.Enqueue(now);
                return true;
            }
        }
    }
}
