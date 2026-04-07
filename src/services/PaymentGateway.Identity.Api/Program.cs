using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentGateway.Contracts.Auth;
using PaymentGateway.Contracts.Organizations;
using PaymentGateway.Contracts.Users;
using PaymentGateway.Domain.Constants;
using PaymentGateway.Identity.Api.Data;
using PaymentGateway.Identity.Api.Middleware;
using PaymentGateway.Identity.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("paymentdb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

// Tenant context
builder.Services.AddScoped<TenantContext>();

// Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<OrganizationService>();

// JWT Authentication
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
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

app.MapDefaultEndpoints();

// --- Auth Endpoints (public) ---
app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    try
    {
        var result = await auth.Register(request);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).WithTags("Auth");

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    try
    {
        var result = await auth.Login(request);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
}).WithTags("Auth");

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, AuthService auth) =>
{
    try
    {
        var result = await auth.Refresh(request);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
}).WithTags("Auth");

app.MapPost("/api/auth/logout", async (RefreshTokenRequest request, AuthService auth) =>
{
    await auth.Logout(request.RefreshToken);
    return Results.Ok();
}).RequireAuthorization().WithTags("Auth");

// --- User Endpoints ---
app.MapGet("/api/users/me", async (UserService users) =>
{
    var user = await users.GetCurrentUser();
    return Results.Ok(user);
}).RequireAuthorization().WithTags("Users");

app.MapPut("/api/users/me", async (UpdateProfileRequest request, UserService users) =>
{
    var user = await users.UpdateProfile(request);
    return Results.Ok(user);
}).RequireAuthorization().WithTags("Users");

app.MapGet("/api/users", async (UserService users) =>
{
    var result = await users.ListOrgUsers();
    return Results.Ok(result);
}).RequireAuthorization(policy => policy.RequireRole("CustomerAdmin", "PlatformAdmin")).WithTags("Users");

app.MapPost("/api/users/invite", async (InviteUserRequest request, UserService users) =>
{
    try
    {
        var user = await users.InviteUser(request);
        return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization(policy => policy.RequireRole("CustomerAdmin", "PlatformAdmin")).WithTags("Users");

// --- Organization Endpoints ---
app.MapGet("/api/organizations/me", async (OrganizationService orgs) =>
{
    var org = await orgs.GetCurrentOrganization();
    return Results.Ok(org);
}).RequireAuthorization().WithTags("Organizations");

app.MapPut("/api/organizations/me", async (UpdateOrganizationRequest request, OrganizationService orgs) =>
{
    var org = await orgs.UpdateOrganization(request);
    return Results.Ok(org);
}).RequireAuthorization(policy => policy.RequireRole("CustomerAdmin", "PlatformAdmin")).WithTags("Organizations");

// Auto-migrate and seed (retry up to 30s for SQL Server to be ready)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
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

namespace PaymentGateway.Identity.Api
{
    public partial class Program { }
}
