using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Middleware;
using PaymentGateway.Gateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core
builder.Services.AddDbContext<GatewayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("paymentdb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "gateway")));

// Tenant context
builder.Services.AddScoped<TenantContext>();

// Services
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddHostedService<WebhookDispatcherService>();

// JWT + ApiKey Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "PaymentGatewayDefaultSecretKey_ChangeInProduction!2024";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = PlatformConstants.JwtIssuer,
            ValidAudience = PlatformConstants.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "API Key authentication"
    });

    options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        },
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("ApiKey"),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.MapDefaultEndpoints();
app.MapControllers();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed — database may not be available yet");
    }
}

app.Run();

namespace PaymentGateway.Gateway.Api
{
    public partial class Program { }
}
