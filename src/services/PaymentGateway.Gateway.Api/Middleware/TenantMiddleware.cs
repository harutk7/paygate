using PaymentGateway.Gateway.Api.Data;

namespace PaymentGateway.Gateway.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            tenantContext.SetFromClaimsPrincipal(context.User);
        }

        await _next(context);
    }
}
