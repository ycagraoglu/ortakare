using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.GetEventExports;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.GalleryExports;

public sealed class GetEventExportsEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetEventExportsEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEventExports_returns_paginated_history_and_signed_url_only_for_completed(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedExportsAsync(ownerId, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{eventId}/exports?page=1&pageSize=2",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<GetEventExportsResponse>>(cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal(GalleryExportStatus.Completed, result.Data.Items[0].Status);
        Assert.NotNull(result.Data.Items[0].DownloadUrl);
        Assert.NotNull(result.Data.Items[0].DownloadUrlExpiresAtUtc);
        Assert.Equal(GalleryExportStatus.Processing, result.Data.Items[1].Status);
        Assert.Null(result.Data.Items[1].DownloadUrl);
        Assert.Null(result.Data.Items[1].DownloadUrlExpiresAtUtc);
    }

    [Fact]
    public async Task GetEventExports_returns_not_found_for_another_owner(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var eventId = await SeedExportsAsync(ownerId, cancellationToken);

        var response = await otherClient.GetAsync(
            $"/api/events/{eventId}/exports",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEventExports_rejects_invalid_pagination(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedExportsAsync(ownerId, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{eventId}/exports?page=0&pageSize=101",
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> SeedExportsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var eventId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Export History Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = now
        });

        dbContext.GalleryExports.AddRange(
            new GalleryExport
            {
                Id = Guid.CreateVersion7(),
                EventId = eventId,
                Status = GalleryExportStatus.Pending,
                PhotoCount = 1,
                CreatedAtUtc = now.AddMinutes(-3)
            },
            new GalleryExport
            {
                Id = Guid.CreateVersion7(),
                EventId = eventId,
                Status = GalleryExportStatus.Processing,
                PhotoCount = 2,
                CreatedAtUtc = now.AddMinutes(-2)
            },
            new GalleryExport
            {
                Id = Guid.CreateVersion7(),
                EventId = eventId,
                Status = GalleryExportStatus.Completed,
                PhotoCount = 3,
                StorageKey = $"exports/{eventId}/{Guid.NewGuid():N}.zip",
                CreatedAtUtc = now.AddMinutes(-1),
                CompletedAtUtc = now
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return eventId;
    }

    private static async Task<Guid> AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"export-history-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Export History Owner", email, password),
            cancellationToken);
        var registerResult = await registerResponse.Content
            .ReadFromJsonAsync<ApiResult<RegisterResponse>>(cancellationToken);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.NotNull(registerResult?.Data);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);

        return registerResult.Data.Id;
    }
}