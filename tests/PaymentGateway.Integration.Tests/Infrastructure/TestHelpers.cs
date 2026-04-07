using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentGateway.Contracts.Auth;

namespace PaymentGateway.Integration.Tests.Infrastructure;

public static class TestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<LoginResponse> RegisterUser(
        HttpClient client,
        string orgName = "Test Organization",
        string email = "test@example.com",
        string password = "TestPass123!",
        string firstName = "Test",
        string lastName = "User")
    {
        var request = new RegisterRequest(orgName, email, password, firstName, lastName);
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
    }

    public static async Task<LoginResponse> LoginUser(
        HttpClient client,
        string email = "test@example.com",
        string password = "TestPass123!")
    {
        var request = new LoginRequest(email, password);
        var response = await client.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
    }

    public static async Task<LoginResponse> LoginAdmin(HttpClient client)
    {
        return await LoginUser(client, "admin@paygate.local", "Admin123!");
    }

    public static void SetAuthToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task<T?> ReadJsonResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}
