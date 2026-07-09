using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StockTrace.IntegrationTests.Authentication;

internal static class TestAuthenticationExtensions
{
    public static async Task<string> LoginAsAdminAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "Admin@12345"
        });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        ArgumentNullException.ThrowIfNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login.AccessToken;
    }

    private sealed record LoginResponse(string AccessToken);
}
