using System.Net;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.IntegrationTests.Auth;

public sealed class RegisterEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly HttpClient _client;

    public RegisterEndpointTests(OrtakareApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_creates_user(CancellationToken cancellationToken)
    {
        var request = new RegisterRequest(
            "Yusuf Çağraoğlu",
            "yusuf@example.com",
            "StrongPassword123!");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<RegisterResponse>>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(request.Email, result.Data.Email);
        Assert.Equal(request.DisplayName, result.Data.DisplayName);
    }

    [Fact]
    public async Task Register_returns_conflict_for_existing_email(CancellationToken cancellationToken)
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest("Yusuf", email, "StrongPassword123!");

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request,
            cancellationToken);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request with { Email = email.ToUpperInvariant() },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_returns_bad_request_for_invalid_input(CancellationToken cancellationToken)
    {
        var request = new RegisterRequest("", "not-an-email", "123");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
