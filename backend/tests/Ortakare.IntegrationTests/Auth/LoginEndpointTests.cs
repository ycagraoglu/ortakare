using System.Net;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.IntegrationTests.Auth;

public sealed class LoginEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(OrtakareApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_returns_access_token_for_valid_credentials(
        CancellationToken cancellationToken)
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Yusuf", email, password),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email.ToUpperInvariant(), password),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var result = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        Assert.True(result.Data.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(email, result.Data.Email);
    }

    [Fact]
    public async Task Login_returns_unauthorized_for_wrong_password(
        CancellationToken cancellationToken)
    {
        var email = $"wrong-password-{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Yusuf", email, "StrongPassword123!"),
            cancellationToken);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "WrongPassword123!"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_unauthorized_for_unknown_email(
        CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest($"unknown-{Guid.NewGuid():N}@example.com", "StrongPassword123!"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
