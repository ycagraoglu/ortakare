using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.UpdateEvent;

namespace Ortakare.IntegrationTests.Events;

public sealed class UpdateEventEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public UpdateEventEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_changes_owned_event(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, $"update-{Guid.NewGuid():N}@example.com", cancellationToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest("Old Title", new DateTime(2027, 1, 1, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(cancellationToken);
        Assert.NotNull(created?.Data);

        var response = await client.PutAsJsonAsync(
            $"/api/events/{created.Data.Id}",
            new UpdateEventRequest("New Title", new DateTime(2027, 2, 1, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<UpdateEventResponse>>(cancellationToken);
        Assert.Equal("New Title", result?.Data?.Title);
        Assert.True(result?.Data?.UpdatedAtUtc > DateTime.MinValue);
    }

    [Fact]
    public async Task Update_returns_not_found_for_another_users_event(CancellationToken cancellationToken)
    {
        using var owner = _factory.CreateClient();
        using var other = _factory.CreateClient();
        await AuthenticateAsync(owner, $"owner-{Guid.NewGuid():N}@example.com", cancellationToken);
        await AuthenticateAsync(other, $"other-{Guid.NewGuid():N}@example.com", cancellationToken);

        var createResponse = await owner.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest("Private", new DateTime(2027, 1, 1, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResult<CreateEventResponse>>(cancellationToken);

        var response = await other.PutAsJsonAsync(
            $"/api/events/{created!.Data!.Id}",
            new UpdateEventRequest("Changed", new DateTime(2027, 2, 1, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_returns_bad_request_for_invalid_input(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, $"invalid-{Guid.NewGuid():N}@example.com", cancellationToken);

        var response = await client.PutAsJsonAsync(
            $"/api/events/{Guid.NewGuid()}",
            new UpdateEventRequest("", DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        const string password = "StrongPassword123!";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Owner", email, password), cancellationToken);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), cancellationToken);
        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Data!.AccessToken);
    }
}