using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CaseFlow.IntegrationTests;

public class AuthFlowTests : IClassFixture<CaseFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(CaseFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Then_Login_Returns_Jwt_Token()
    {
        // Arrange
        var email = $"max-{Guid.NewGuid():N}@example.com";

        var registerBody = new
        {
            email,
            password = "Test123!Test123!",
            name = "Max Mustermann",
            role = "Erfasser",
            departmentId = (int?)null
        };

        // Act: Register
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register", registerBody);

        // Assert Register
        Assert.True(registerResp.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Register failed: {(int)registerResp.StatusCode} {await registerResp.Content.ReadAsStringAsync()}");

        // Act: Login
        var loginBody = new { email, password = "Test123!Test123!" };
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginBody);

        // Assert Login
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var json = await loginResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("token", out var tokenEl), $"Login response missing token: {json}");
        var token = tokenEl.GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}