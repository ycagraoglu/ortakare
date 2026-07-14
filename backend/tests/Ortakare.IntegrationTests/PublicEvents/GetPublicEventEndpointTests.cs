using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.PublicEvents.GetPublicEvent;

namespace Ortakare.IntegrationTests.PublicEvents;

public sealed class GetPublicEventEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetPublicEventEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPublicEvent_returns_public_event_information_without_authentication(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var publicClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, cancellationToken);
        var createdEvent = await CreateEventAsync(ownerClient, cancellationToken);

        var response = await publicClient.GetAsync(
            $"/api/public/events/{createdEvent.GalleryToken}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetPublicEventResponse>>(
            cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Equal("Ayşe & Mehmet", result.Data.Title);
        Assert.True(result.Data.UploadsEnabled);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");

        Assert.False(data.TryGetProperty("id", out _));
        Assert.False(data.TryGetProperty("ownerUserId", out _));
        Assert.False(data.TryGetProperty("galleryToken", out _));
    }

    [Fact]
    public async Task GetPublicEvent_returns_not_found_for_unknown_gallery_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/public/events/unknown-gallery-token",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicEvent_returns_closed_event_with_uploads_disabled(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var publicClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, cancellationToken);
        var createdEvent = await CreateEventAsync(ownerClient, cancellationToken);

        var closeResponse = await ownerClient.PostAsync(
            $"/api/events/{createdEvent.Id}/close",
            content: null,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var response = await publicClient.GetAsync(
            $"/api/public/events/{createdEvent.GalleryToken}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetPublicEventResponse>>(
            cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.False(result.Data.UploadsEnabled);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"public-event-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Event Owner", email, password),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);
    }

    private static async Task<CreateEventResponse> CreateEventAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(
                "Ayşe & Mehmet",
                new DateTime(2027, 6, 12, 16, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(
            cancellationToken);

        Assert.NotNull(result?.Data);
        return result.Data;
    }
}
