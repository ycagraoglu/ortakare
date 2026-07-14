using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Participants.JoinEvent;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Participants;

public sealed class JoinEventEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public JoinEventEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinEvent_creates_participant_and_stores_only_token_hash(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsync(ownerClient, cancellationToken);
        var galleryToken = await CreateEventAsync(ownerClient, cancellationToken);

        using var guestClient = _factory.CreateClient();
        var response = await guestClient.PostAsJsonAsync(
            $"/api/public/events/{galleryToken}/participants",
            new JoinEventRequest("  Yusuf  "),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<JoinEventResponse>>(
            cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Equal("Yusuf", result.Data.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.ParticipantToken));

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        var participant = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        Assert.NotEqual(result.Data.ParticipantToken, participant.TokenHash);
        Assert.Equal(64, participant.TokenHash.Length);
    }

    [Fact]
    public async Task JoinEvent_returns_conflict_when_uploads_are_closed(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsync(ownerClient, cancellationToken);
        var createResult = await CreateEventWithIdAsync(ownerClient, cancellationToken);

        var closeResponse = await ownerClient.PostAsync(
            $"/api/events/{createResult.Id}/close",
            content: null,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        using var guestClient = _factory.CreateClient();
        var response = await guestClient.PostAsJsonAsync(
            $"/api/public/events/{createResult.GalleryToken}/participants",
            new JoinEventRequest("Misafir"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task JoinEvent_returns_bad_request_for_invalid_display_name(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsync(ownerClient, cancellationToken);
        var galleryToken = await CreateEventAsync(ownerClient, cancellationToken);

        using var guestClient = _factory.CreateClient();
        var response = await guestClient.PostAsJsonAsync(
            $"/api/public/events/{galleryToken}/participants",
            new JoinEventRequest(" "),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task JoinEvent_returns_not_found_for_unknown_gallery_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/public/events/unknown-token/participants",
            new JoinEventRequest("Misafir"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"participant-owner-{Guid.NewGuid():N}@example.com";
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
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);
    }

    private static async Task<string> CreateEventAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var result = await CreateEventWithIdAsync(client, cancellationToken);
        return result.GalleryToken;
    }

    private static async Task<CreateEventResponse> CreateEventWithIdAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(
                "Katılım Testi",
                new DateTime(2027, 6, 10, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(
            cancellationToken);
        Assert.NotNull(result?.Data);
        return result.Data;
    }
}
