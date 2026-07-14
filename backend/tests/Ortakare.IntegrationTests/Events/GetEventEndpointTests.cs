using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetEvent;

namespace Ortakare.IntegrationTests.Events;

public sealed class GetEventEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetEventEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEvent_returns_owned_event(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, $"owner-{Guid.NewGuid():N}@example.com", cancellationToken);
        var createdEvent = await CreateEventAsync(client, cancellationToken);

        var response = await client.GetAsync($"/api/events/{createdEvent.Id}", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetEventResponse>>(cancellationToken);
        Assert.NotNull(result?.Data);
        Assert.Equal(createdEvent.Id, result.Data.Id);
        Assert.Equal(createdEvent.Title, result.Data.Title);
        Assert.Equal(createdEvent.GalleryToken, result.Data.GalleryToken);
    }

    [Fact]
    public async Task GetEvent_returns_not_found_for_another_users_event(CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(ownerClient, $"owner-{Guid.NewGuid():N}@example.com", cancellationToken);
        await AuthenticateAsync(otherClient, $"other-{Guid.NewGuid():N}@example.com", cancellationToken);
        var createdEvent = await CreateEventAsync(ownerClient, cancellationToken);

        var response = await otherClient.GetAsync($"/api/events/{createdEvent.Id}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_returns_not_found_for_unknown_event(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, $"owner-{Guid.NewGuid():N}@example.com", cancellationToken);

        var response = await client.GetAsync($"/api/events/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_returns_unauthorized_without_access_token(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/events/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
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

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);
        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);
    }

    private static async Task<CreateEventResponse> CreateEventAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest("Ayse & Mehmet", new DateTime(2027, 7, 18, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(cancellationToken);
        Assert.NotNull(result?.Data);
        return result.Data;
    }
}
