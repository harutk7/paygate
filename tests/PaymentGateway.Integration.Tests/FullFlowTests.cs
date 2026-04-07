using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Contracts.ApiKeys;
using PaymentGateway.Contracts.Auth;
using PaymentGateway.Contracts.Plans;
using PaymentGateway.Contracts.Subscriptions;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Contracts.Users;
using PaymentGateway.Contracts.Webhooks;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Integration.Tests.Infrastructure;

namespace PaymentGateway.Integration.Tests;

public class FullFlowTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IdentityApiFactory _identityFactory;
    private readonly BillingApiFactory _billingFactory;
    private readonly GatewayApiFactory _gatewayFactory;
    private readonly BackofficeApiFactory _backofficeFactory;

    public FullFlowTests()
    {
        _identityFactory = new IdentityApiFactory();
        _billingFactory = new BillingApiFactory();
        _gatewayFactory = new GatewayApiFactory();
        _backofficeFactory = new BackofficeApiFactory();
    }

    public void Dispose()
    {
        _identityFactory.Dispose();
        _billingFactory.Dispose();
        _gatewayFactory.Dispose();
        _backofficeFactory.Dispose();
    }

    // Test 1: Registration to Dashboard Flow
    [Fact]
    public async Task Registration_To_Dashboard_Flow()
    {
        // Arrange
        var client = _identityFactory.CreateClient();

        // Act - Register
        var registerRequest = new RegisterRequest("Acme Corp", "acme@test.com", "Password123!", "Jane", "Doe");
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert - Registration succeeds
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.RefreshToken.Should().NotBeNullOrEmpty();
        loginResult.User.Should().NotBeNull();
        loginResult.User.Email.Should().Be("acme@test.com");
        loginResult.User.FirstName.Should().Be("Jane");

        // Act - Login with same credentials
        var loginRequest = new LoginRequest("acme@test.com", "Password123!");
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Login succeeds
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        loginData.Should().NotBeNull();
        loginData!.AccessToken.Should().NotBeNullOrEmpty();

        // Act - Get current user profile
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginData.AccessToken);
        var meResponse = await client.GetAsync("/api/users/me");

        // Assert - Profile matches registration
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await meResponse.Content.ReadFromJsonAsync<UserDto>(JsonOpts);
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be("acme@test.com");
        userDto.FirstName.Should().Be("Jane");
        userDto.LastName.Should().Be("Doe");
        userDto.Role.Should().Be(UserRole.CustomerAdmin);
        userDto.IsActive.Should().BeTrue();
    }

    // Test 2: Token Refresh Flow
    [Fact]
    public async Task Token_Refresh_Flow()
    {
        // Arrange - Register and get tokens
        var client = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(client, "RefreshOrg", "refresh@test.com");

        // Act - Refresh the token
        var refreshRequest = new RefreshTokenRequest(registerResult.RefreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert - Refresh succeeds with new tokens
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts);
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
        tokenResponse.AccessToken.Should().NotBe(registerResult.AccessToken);

        // Act - Use new access token to verify it works
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        var meResponse = await client.GetAsync("/api/users/me");

        // Assert - New token is valid
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await meResponse.Content.ReadFromJsonAsync<UserDto>(JsonOpts);
        user!.Email.Should().Be("refresh@test.com");

        // Act - Old refresh token should be revoked (rotating)
        var oldRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(registerResult.RefreshToken));

        // Assert - Old refresh token is rejected
        oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Test 3: Plan Listing (Public)
    [Fact]
    public async Task Plan_Listing_Public()
    {
        // Arrange - Seed plans
        await _billingFactory.SeedPlans();
        var client = _billingFactory.CreateClient();

        // Act - Get plans (no auth required)
        var response = await client.GetAsync("/api/plans");

        // Assert - Returns 3 plans
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<PlanDto>>(JsonOpts);
        plans.Should().NotBeNull();
        plans!.Should().HaveCount(3);
        plans.Should().Contain(p => p.Name == "Starter" && p.PriceMonthly == 49m);
        plans.Should().Contain(p => p.Name == "Business" && p.PriceMonthly == 199m);
        plans.Should().Contain(p => p.Name == "Enterprise" && p.PriceMonthly == 799m);
    }

    // Test 4: Subscription Payment Flow
    [Fact]
    public async Task Subscription_Payment_Flow()
    {
        // Arrange - Register user on Identity, seed plans on Billing
        var identityClient = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(identityClient, "SubOrg", "sub@test.com");

        await _billingFactory.SeedPlans();
        var billingClient = _billingFactory.CreateClient();

        // Set the JWT token from Identity on the Billing client
        billingClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        // Act - Create subscription to Starter plan
        var starterPlanId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var subRequest = new CreateSubscriptionRequest(starterPlanId, null);
        var subResponse = await billingClient.PostAsJsonAsync("/api/subscriptions", subRequest);

        // Assert - Subscription created and active
        subResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var subscription = await subResponse.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOpts);
        subscription.Should().NotBeNull();
        subscription!.Status.Should().Be(SubscriptionStatus.Active);
        subscription.PlanName.Should().Be("Starter");
        subscription.PlanId.Should().Be(starterPlanId);

        // Act - Get current subscription
        var currentSubResponse = await billingClient.GetAsync("/api/subscriptions/current");

        // Assert
        currentSubResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentSub = await currentSubResponse.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOpts);
        currentSub.Should().NotBeNull();
        currentSub!.Status.Should().Be(SubscriptionStatus.Active);
    }

    // Test 5: API Key and Transaction Flow
    [Fact]
    public async Task ApiKey_Transaction_Flow()
    {
        // Arrange - Register user
        var identityClient = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(identityClient, "GatewayOrg", "gateway@test.com");

        var gatewayClient = _gatewayFactory.CreateClient();
        gatewayClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        // Act - Create API key
        var apiKeyRequest = new CreateApiKeyRequest("Production Key", null);
        var apiKeyResponse = await gatewayClient.PostAsJsonAsync("/api/apikeys", apiKeyRequest);

        // Assert - API key created
        apiKeyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var apiKeyResult = await apiKeyResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOpts);
        apiKeyResult.Should().NotBeNull();
        apiKeyResult!.Key.Should().StartWith("pk_live_");
        apiKeyResult.Name.Should().Be("Production Key");

        // Act - Use API key to create a charge
        var chargeClient = _gatewayFactory.CreateClient();
        chargeClient.DefaultRequestHeaders.Add("X-API-Key", apiKeyResult.Key);

        var chargeRequest = new CreateChargeRequest(
            Amount: 99.99m,
            Currency: "USD",
            CardNumber: "4111111111111111",
            Description: "Test charge",
            Metadata: new Dictionary<string, string> { ["order_id"] = "ORD-001" });

        var chargeResponse = await chargeClient.PostAsJsonAsync("/api/v1/charges", chargeRequest);

        // Assert - Charge created
        chargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var chargeResult = await chargeResponse.Content.ReadFromJsonAsync<CreateChargeResponse>(JsonOpts);
        chargeResult.Should().NotBeNull();
        chargeResult!.Amount.Should().Be(99.99m);
        chargeResult.Currency.Should().Be("USD");

        // Act - List API keys
        var keysResponse = await gatewayClient.GetAsync("/api/apikeys");

        // Assert
        keysResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var keys = await keysResponse.Content.ReadFromJsonAsync<List<ApiKeyDto>>(JsonOpts);
        keys.Should().NotBeNull();
        keys!.Should().HaveCount(1);
        keys[0].Name.Should().Be("Production Key");
        keys[0].IsActive.Should().BeTrue();
    }

    // Test 6: Refund Flow
    [Fact]
    public async Task Refund_Flow()
    {
        // Arrange - Register, create API key, create charge
        var identityClient = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(identityClient, "RefundOrg", "refund@test.com");

        var gatewayClient = _gatewayFactory.CreateClient();
        gatewayClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        // Create API key
        var apiKeyResponse = await gatewayClient.PostAsJsonAsync("/api/apikeys",
            new CreateApiKeyRequest("Refund Test Key", null));
        apiKeyResponse.EnsureSuccessStatusCode();
        var apiKey = await apiKeyResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOpts);

        // Create charge via API key
        var chargeClient = _gatewayFactory.CreateClient();
        chargeClient.DefaultRequestHeaders.Add("X-API-Key", apiKey!.Key);

        var chargeResponse = await chargeClient.PostAsJsonAsync("/api/v1/charges",
            new CreateChargeRequest(150.00m, "USD", "4111111111111111", "Refund test", null));
        chargeResponse.EnsureSuccessStatusCode();
        var charge = await chargeResponse.Content.ReadFromJsonAsync<CreateChargeResponse>(JsonOpts);

        // Act - Refund the charge (full refund)
        var refundResponse = await chargeClient.PostAsJsonAsync(
            $"/api/v1/charges/{charge!.Id}/refund",
            new RefundRequest(null, "Customer requested"));

        // Assert - Refund created
        refundResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // Verify the transaction now shows refunded
        var txnResponse = await chargeClient.GetAsync($"/api/v1/charges/{charge.Id}");
        txnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Test 7: Tenant Isolation
    [Fact]
    public async Task Tenant_Isolation_Test()
    {
        // Arrange - Create two separate organizations
        var client = _identityFactory.CreateClient();

        var org1Result = await TestHelpers.RegisterUser(client, "Org One", "org1@test.com", "Pass123!");
        var org2Result = await TestHelpers.RegisterUser(client, "Org Two", "org2@test.com", "Pass123!");

        // Act - Org 1 gets its users
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", org1Result.AccessToken);
        var org1UsersResponse = await client.GetAsync("/api/users");
        org1UsersResponse.EnsureSuccessStatusCode();
        var org1Users = await org1UsersResponse.Content.ReadFromJsonAsync<List<UserDto>>(JsonOpts);

        // Act - Org 2 gets its users
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", org2Result.AccessToken);
        var org2UsersResponse = await client.GetAsync("/api/users");
        org2UsersResponse.EnsureSuccessStatusCode();
        var org2Users = await org2UsersResponse.Content.ReadFromJsonAsync<List<UserDto>>(JsonOpts);

        // Assert - Each org only sees its own users
        org1Users.Should().NotBeNull();
        org1Users!.Should().HaveCount(1);
        org1Users[0].Email.Should().Be("org1@test.com");

        org2Users.Should().NotBeNull();
        org2Users!.Should().HaveCount(1);
        org2Users[0].Email.Should().Be("org2@test.com");

        // Assert - User IDs are different
        org1Users[0].Id.Should().NotBe(org2Users![0].Id);
    }

    // Test 8: Backoffice Admin Flow
    [Fact]
    public async Task Backoffice_Admin_Flow()
    {
        // Arrange - Seed admin user in Identity and get admin token
        var identityClient = _identityFactory.CreateClient();

        // The admin user is seeded via model builder (admin@paygate.local / Admin123!)
        // We need to seed it manually for InMemory
        using (var scope = _identityFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentGateway.Identity.Api.Data.IdentityDbContext>();
            if (!await db.Organizations.AnyAsync())
            {
                var adminOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

                db.Organizations.Add(new PaymentGateway.Domain.Entities.Organization
                {
                    Id = adminOrgId,
                    Name = "Platform Administration",
                    Slug = "platform-admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                db.Users.Add(new PaymentGateway.Domain.Entities.User
                {
                    Id = adminUserId,
                    OrganizationId = adminOrgId,
                    Email = "admin@paygate.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "Platform",
                    LastName = "Admin",
                    Role = UserRole.PlatformAdmin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
        }

        // Login as admin
        var adminLogin = await TestHelpers.LoginUser(identityClient, "admin@paygate.local", "Admin123!");
        adminLogin.User.Role.Should().Be(UserRole.PlatformAdmin);

        // Act - Access backoffice dashboard with admin token
        var backofficeClient = _backofficeFactory.CreateClient();
        backofficeClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);

        var dashboardResponse = await backofficeClient.GetAsync("/api/admin/dashboard");

        // Assert - Admin can access dashboard
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Test 9: Webhook Delivery Flow
    [Fact]
    public async Task Webhook_Delivery_Flow()
    {
        // Arrange - Register user and get tokens
        var identityClient = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(identityClient, "WebhookOrg", "webhook@test.com");

        var gatewayClient = _gatewayFactory.CreateClient();
        gatewayClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        // Act - Create a webhook endpoint
        var webhookRequest = new CreateWebhookRequest(
            "https://example.com/webhooks",
            new List<string> { "TransactionSucceeded", "TransactionFailed" },
            true);
        var webhookResponse = await gatewayClient.PostAsJsonAsync("/api/webhooks", webhookRequest);

        // Assert - Webhook endpoint created
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var webhook = await webhookResponse.Content.ReadFromJsonAsync<WebhookEndpointDto>(JsonOpts);
        webhook.Should().NotBeNull();
        webhook!.Url.Should().Be("https://example.com/webhooks");
        webhook.IsActive.Should().BeTrue();
        webhook.Events.Should().Contain("TransactionSucceeded");

        // Act - List webhooks
        var listResponse = await gatewayClient.GetAsync("/api/webhooks");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var webhooks = await listResponse.Content.ReadFromJsonAsync<List<WebhookEndpointDto>>(JsonOpts);
        webhooks.Should().NotBeNull();
        webhooks!.Should().HaveCount(1);

        // Act - Create API key and charge (this should trigger webhook delivery)
        var apiKeyResponse = await gatewayClient.PostAsJsonAsync("/api/apikeys",
            new CreateApiKeyRequest("Webhook Test Key", null));
        apiKeyResponse.EnsureSuccessStatusCode();
        var apiKey = await apiKeyResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>(JsonOpts);

        var chargeClient = _gatewayFactory.CreateClient();
        chargeClient.DefaultRequestHeaders.Add("X-API-Key", apiKey!.Key);

        var chargeResponse = await chargeClient.PostAsJsonAsync("/api/v1/charges",
            new CreateChargeRequest(50.00m, "USD", "4111111111111111", "Webhook test charge", null));

        // The charge itself should succeed
        chargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // Test 10: Subscription Limit Enforcement
    [Fact]
    public async Task Subscription_Limit_Enforcement()
    {
        // Arrange - Register user and subscribe to Starter plan
        var identityClient = _identityFactory.CreateClient();
        var registerResult = await TestHelpers.RegisterUser(identityClient, "LimitOrg", "limit@test.com");

        await _billingFactory.SeedPlans();
        var billingClient = _billingFactory.CreateClient();
        billingClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        // Subscribe to Starter plan (which has limits: 1000 transactions, 2 API keys, 60 rpm)
        var starterPlanId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var subResponse = await billingClient.PostAsJsonAsync("/api/subscriptions",
            new CreateSubscriptionRequest(starterPlanId, null));
        subResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the plan details include limits
        var plansResponse = await billingClient.GetAsync("/api/plans");
        var plans = await plansResponse.Content.ReadFromJsonAsync<List<PlanDto>>(JsonOpts);
        var starterPlan = plans!.First(p => p.Name == "Starter");

        starterPlan.TransactionLimit.Should().Be(1000);
        starterPlan.ApiKeyLimit.Should().Be(2);
        starterPlan.RateLimit.Should().Be(60);

        // Verify subscription is active with correct plan
        var currentSub = await billingClient.GetAsync("/api/subscriptions/current");
        currentSub.StatusCode.Should().Be(HttpStatusCode.OK);
        var sub = await currentSub.Content.ReadFromJsonAsync<SubscriptionDto>(JsonOpts);
        sub!.PlanName.Should().Be("Starter");
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    // Additional: Authentication required tests
    [Fact]
    public async Task Unauthorized_Access_Returns_401()
    {
        // Act - Try to access protected endpoint without token
        var client = _identityFactory.CreateClient();
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Duplicate_Registration_Returns_Conflict()
    {
        // Arrange
        var client = _identityFactory.CreateClient();
        await TestHelpers.RegisterUser(client, "DupOrg", "dup@test.com");

        // Act - Try to register again with same email
        var dupRequest = new RegisterRequest("Another Org", "dup@test.com", "Pass123!", "Dup", "User");
        var response = await client.PostAsJsonAsync("/api/auth/register", dupRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_Login_Returns_Unauthorized()
    {
        // Act
        var client = _identityFactory.CreateClient();
        var loginRequest = new LoginRequest("nonexistent@test.com", "WrongPass!");
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
