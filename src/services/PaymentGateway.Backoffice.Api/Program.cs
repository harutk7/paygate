using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Backoffice.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<BackofficeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("paymentdb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "backoffice")));

builder.Services.AddDbContext<ReadOnlyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("paymentdb")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "PaymentGateway",
            ValidateAudience = true,
            ValidAudience = "PaymentGateway",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "PaymentGatewayDefaultSecretKey_ChangeInProduction!2024")),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin"));
});

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CustomerManagementService>();
builder.Services.AddScoped<PlanManagementService>();
builder.Services.AddScoped<RevenueService>();
builder.Services.AddScoped<AuditLogService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate (retry up to 30s for SQL Server to be ready)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackofficeDbContext>();
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

namespace PaymentGateway.Backoffice.Api
{
    public partial class Program { }
}
