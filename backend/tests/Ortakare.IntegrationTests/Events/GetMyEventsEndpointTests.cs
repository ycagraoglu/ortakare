using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetMyEvents;

namespace Ortakare.IntegrationTests.Events;

public sealed class GetMyEventsEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetMyEventsEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyEvents_returns_only_current_users_events_in_date_order(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, $"owner-{Guid.NewGuid():N}@example.com", cancellationToken);
        await AuthenticateAsync(otherClient, $"other-{Guid.NewGuid():N}@example.com", cancellationToken);

        await CreateEventAsync(ownerClient, "Older Event", new DateTime(2027, 1, 10, 18, 0, 0, DateTimeKind.Utc), cancellationToken);
        await CreateEventAsync(ownerClient, "Newest Event", new DateTime(2027, 3, 10, 18, 0, 0, DateTimeKind.Utc), cancellationToken);
        await CreateEventAsync(ownerClient, "Middle Event", new DateTime(2027, 2, 10, 18, 0, 0, DateTimeKind.Utc), cancellationToken);
        await CreateEventAsync(otherClient, "Other User Event", new DateTime(2027, 4, 10, 18, 0, 0, DateTimeKind.Utc), cancellationToken);

        var response = await ownerClient.GetAsync(
            "/api/events?page=1&pageSize=2",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetMyEventsResponse>>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal("Newest Event", result.Data.Items[0].Title);
        Assert.Equal("Middle Event", result.Data.Items[1].Title);
        Assert.DoesNotContain(result.Data.Items, x => x.Title == "Other User Event");
    }

    [Fact]
    public async Task GetMyEvents_returns_unauthorized_without_access_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEvents_returns_bad_request_for_invalid_paging(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, $"paging-{Guid.NewGuid():N}@example.com", cancellationToken);

        var response = await client.GetAsync(
            "/api/events?page=0&pageSize=101",
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string email,
        CancellationToken cancellationToken)
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

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);
    }

    private static async Task CreateEventAsync(
        HttpClient client,
        string title,
        DateTime eventDateUtc,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(title, eventDateUtc),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
