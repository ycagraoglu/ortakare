using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Participants.JoinEvent;
using Ortakare.Api.Features.Photos.UploadPhoto;

namespace Ortakare.IntegrationTests.Photos;

public sealed class UploadPhotoEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public UploadPhotoEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadPhoto_uploads_valid_image_once_and_returns_existing_result_on_retry(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        using var ownerClient = _factory.CreateClient();
        using var guestClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, cancellationToken);
        var galleryToken = await CreateEventAsync(ownerClient, cancellationToken);
        var participantToken = await JoinEventAsync(guestClient, galleryToken, cancellationToken);
        var clientUploadId = Guid.NewGuid();

        var firstResponse = await UploadJpegAsync(
            guestClient,
            galleryToken,
            participantToken,
            clientUploadId,
            cancellationToken);

        var secondResponse = await UploadJpegAsync(
            guestClient,
            galleryToken,
            participantToken,
            clientUploadId,
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(1, _factory.ObjectStorage.UploadCount);

        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<ApiResult<UploadPhotoResponse>>(cancellationToken);
        var secondResult = await secondResponse.Content
            .ReadFromJsonAsync<ApiResult<UploadPhotoResponse>>(cancellationToken);

        Assert.NotNull(firstResult?.Data);
        Assert.NotNull(secondResult?.Data);
        Assert.False(firstResult.Data.AlreadyUploaded);
        Assert.True(secondResult.Data.AlreadyUploaded);
        Assert.Equal(firstResult.Data.PhotoId, secondResult.Data.PhotoId);
        Assert.Equal(clientUploadId, firstResult.Data.ClientUploadId);
        Assert.Equal("image/jpeg", firstResult.Data.ContentType);
    }

    [Fact]
    public async Task UploadPhoto_rejects_participant_token_from_another_event(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var guestClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, cancellationToken);
        var firstGalleryToken = await CreateEventAsync(ownerClient, cancellationToken, "First Event");
        var secondGalleryToken = await CreateEventAsync(ownerClient, cancellationToken, "Second Event");
        var participantToken = await JoinEventAsync(guestClient, firstGalleryToken, cancellationToken);

        var response = await UploadJpegAsync(
            guestClient,
            secondGalleryToken,
            participantToken,
            Guid.NewGuid(),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_rejects_invalid_image_signature(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var guestClient = _factory.CreateClient();

        await AuthenticateAsync(ownerClient, cancellationToken);
        var galleryToken = await CreateEventAsync(ownerClient, cancellationToken);
        var participantToken = await JoinEventAsync(guestClient, galleryToken, cancellationToken);

        using var request = CreateUploadRequest(
            galleryToken,
            participantToken,
            Guid.NewGuid(),
            new byte[] { 0x01, 0x02, 0x03, 0x04 },
            "fake.jpg",
            "image/jpeg");

        var response = await guestClient.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"photo-owner-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Photo Owner", email, password),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

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
    }

    private static async Task<string> CreateEventAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string title = "Photo Event")
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(
                title,
                new DateTime(2027, 8, 10, 18, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<CreateEventResponse>>(cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result?.Data);
        return result.Data.GalleryToken;
    }

    private static async Task<string> JoinEventAsync(
        HttpClient client,
        string galleryToken,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/public/events/{galleryToken}/participants",
            new JoinEventRequest("Guest Photographer"),
            cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<JoinEventResponse>>(cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result?.Data);
        return result.Data.ParticipantToken;
    }

    private static async Task<HttpResponseMessage> UploadJpegAsync(
        HttpClient client,
        string galleryToken,
        string participantToken,
        Guid clientUploadId,
        CancellationToken cancellationToken)
    {
        var jpeg = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0,
            0x00, 0x10, 0x4A, 0x46,
            0x49, 0x46, 0x00, 0x01
        };

        using var request = CreateUploadRequest(
            galleryToken,
            participantToken,
            clientUploadId,
            jpeg,
            "photo.jpg",
            "image/jpeg");

        return await client.SendAsync(request, cancellationToken);
    }

    private static HttpRequestMessage CreateUploadRequest(
        string galleryToken,
        string participantToken,
        Guid clientUploadId,
        byte[] bytes,
        string fileName,
        string contentType)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "File", fileName);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/public/events/{galleryToken}/photos")
        {
            Content = multipart
        };

        request.Headers.TryAddWithoutValidation("X-Participant-Token", participantToken);
        request.Headers.TryAddWithoutValidation("X-Client-Upload-Id", clientUploadId.ToString());
        return request;
    }
}