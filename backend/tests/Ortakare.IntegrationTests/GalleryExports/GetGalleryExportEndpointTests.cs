using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.GetGalleryExport;
using Ortakare.Api.Features.GalleryExports.GetGalleryExportDownloadUrl;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.GalleryExports;

public sealed class GetGalleryExportEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetGalleryExportEndpointTests(OrtakareApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_returns_export_status_without_download_url(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedExportAsync(ownerId, GalleryExportStatus.Completed, cancellationToken);

        var response = await client.GetAsync($"/api/events/{seeded.EventId}/exports/{seeded.ExportId}", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetGalleryExportResponse>>(cancellationToken);
        Assert.NotNull(result?.Data);
        Assert.Equal(GalleryExportStatus.Completed, result.Data.Status);
    }

    [Fact]
    public async Task Get_download_url_returns_signed_url_for_completed_export(CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedExportAsync(ownerId, GalleryExportStatus.Completed, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{seeded.EventId}/exports/{seeded.ExportId}/download-url",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetGalleryExportDownloadUrlResponse>>(cancellationToken);
        Assert.NotNull(result?.Data);
        Assert.Equal(seeded.ExportId, result.Data.ExportId);
        Assert.Contains(Uri.EscapeDataString(seeded.StorageKey), result.Data.DownloadUrl);
        Assert.True(result.Data.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Get_download_url_returns_conflict_for_pending_export(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedExportAsync(ownerId, GalleryExportStatus.Pending, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{seeded.EventId}/exports/{seeded.ExportId}/download-url",
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_download_url_returns_not_found_when_completed_export_has_no_storage_key(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedExportAsync(
            ownerId,
            GalleryExportStatus.Completed,
            cancellationToken,
            includeStorageKey: false);

        var response = await client.GetAsync(
            $"/api/events/{seeded.EventId}/exports/{seeded.ExportId}/download-url",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_download_url_returns_not_found_for_another_owner(CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var seeded = await SeedExportAsync(ownerId, GalleryExportStatus.Completed, cancellationToken);

        var response = await otherClient.GetAsync(
            $"/api/events/{seeded.EventId}/exports/{seeded.ExportId}/download-url",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<SeededExport> SeedExportAsync(
        Guid ownerId,
        GalleryExportStatus status,
        CancellationToken cancellationToken,
        bool includeStorageKey = true)
    {
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        var storageKey = $"exports/{eventId}/{exportId}.zip";
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Export Status Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = now
        });
        dbContext.GalleryExports.Add(new GalleryExport
        {
            Id = exportId,
            EventId = eventId,
            Status = status,
            PhotoCount = 3,
            StorageKey = status == GalleryExportStatus.Completed && includeStorageKey ? storageKey : null,
            CreatedAtUtc = now,
            CompletedAtUtc = status == GalleryExportStatus.Completed ? now : null
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SeededExport(eventId, exportId, storageKey);
    }

    private static async Task<Guid> AuthenticateAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var email = $"export-status-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Export Owner", email, password),
            cancellationToken);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResult<RegisterResponse>>(cancellationToken);
        Assert.NotNull(registerResult?.Data);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);
        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);
        return registerResult.Data.Id;
    }

    private sealed record SeededExport(Guid EventId, Guid ExportId, string StorageKey);
}
