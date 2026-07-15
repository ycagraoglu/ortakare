using System.Net;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Logout;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.IntegrationTests.Auth;

public sealed class LogoutEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public LogoutEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Logout_revokes_refresh_token_and_refresh_returns_unauthorized(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var login = await RegisterAndLoginAsync(client, cancellationToken);

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest(login.RefreshToken),
            cancellationToken);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(login.RefreshToken),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_is_idempotent_for_already_revoked_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var login = await RegisterAndLoginAsync(client, cancellationToken);
        var request = new LogoutRequest(login.RefreshToken);

        var firstResponse = await client.PostAsJsonAsync("/api/auth/logout", request, cancellationToken);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/logout", request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_returns_success_for_unknown_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest("unknown-refresh-token"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<LoginResponse> RegisterAndLoginAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Logout User", email, password),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        var result = await loginResponse.Content
            .ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(result?.Data);
        return result.Data;
    }
}
