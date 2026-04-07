using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PaymentGateway.Billing.Api.Data;
using PaymentGateway.Billing.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("paymentdb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "billing")));

// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PaymentGateway",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PaymentGateway",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "PaymentGatewayDefaultSecretKey_ChangeInProduction!2024"))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tenant context
builder.Services.AddScoped<TenantContext>();
builder.Services.AddHttpContextAccessor();

// Services
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<PaymentMethodService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddSingleton<IPaymentProcessorService, AuthorizeNetPaymentService>();

// Background service
builder.Services.AddHostedService<SubscriptionRenewalJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Set tenant context from JWT claims
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();
        tenantContext.SetFromClaimsPrincipal(context.User);
    }
    await next();
});

app.MapControllers();
app.MapDefaultEndpoints();

// Auto-migrate and seed (retry up to 30s for SQL Server to be ready)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var i = 0; i < 10; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt}/10 failed, retrying...", i + 1);
            if (i == 9) throw;
            await Task.Delay(3000);
        }
    }
}

app.Run();

namespace PaymentGateway.Billing.Api
{
    public partial class Program { }
}
