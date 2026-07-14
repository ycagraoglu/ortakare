using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;

namespace Ortakare.IntegrationTests.Events;

public sealed class CreateEventEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly HttpClient _client;

    public CreateEventEndpointTests(OrtakareApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_event_returns_created_for_authenticated_owner(
        CancellationToken cancellationToken)
    {
        var accessToken = await RegisterAndLoginAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = JsonContent.Create(new CreateEventRequest(
                "Ayşe & Mehmet Düğünü",
                DateTime.UtcNow.AddDays(30)))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Ayşe & Mehmet Düğünü", result.Data.Title);
        Assert.True(result.Data.UploadsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.GalleryToken));
    }

    [Fact]
    public async Task Create_event_returns_unauthorized_without_access_token(
        CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest("Ayşe & Mehmet Düğünü", DateTime.UtcNow.AddDays(30)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_event_returns_bad_request_for_invalid_input(
        CancellationToken cancellationToken)
    {
        var accessToken = await RegisterAndLoginAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = JsonContent.Create(new CreateEventRequest("", default))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> RegisterAndLoginAsync(CancellationToken cancellationToken)
    {
        var email = $"event-owner-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Yusuf", email, password),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);
        return loginResult.Data.AccessToken;
    }
}
