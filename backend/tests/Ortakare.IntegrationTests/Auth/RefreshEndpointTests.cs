using System.Net;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.IntegrationTests.Auth;

public sealed class RefreshEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly HttpClient _client;

    public RefreshEndpointTests(OrtakareApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_rotates_token_and_returns_new_tokens(CancellationToken cancellationToken)
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Yusuf", email, password),
            cancellationToken);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(loginResult.Data.RefreshToken),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResult<RefreshResponse>>(
            cancellationToken);

        Assert.NotNull(refreshResult?.Data);
        Assert.NotEqual(loginResult.Data.RefreshToken, refreshResult.Data.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.Data.AccessToken));
    }

    [Fact]
    public async Task Refresh_rejects_reused_token(CancellationToken cancellationToken)
    {
        var email = $"reuse-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Yusuf", email, password),
            cancellationToken);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);

        var request = new RefreshRequest(loginResult.Data.RefreshToken);

        var firstRefresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            request,
            cancellationToken);

        var secondRefresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_unknown_token(CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest("unknown-refresh-token"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}