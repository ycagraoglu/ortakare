using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class ImageFileInspector(IOptions<PhotoUploadOptions> options)
{
    private readonly PhotoUploadOptions _options = options.Value;

    public async Task<ImageFileInfo?> InspectAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (!stream.CanRead || !stream.CanSeek)
        {
            return null;
        }

        var originalPosition = stream.Position;

        try
        {
            var detectedFormat = await DetectFormatAsync(stream, cancellationToken);
            if (detectedFormat is null)
            {
                return null;
            }

            stream.Position = originalPosition;

            if (detectedFormat.Value.ContentType == "image/heic")
            {
                return new ImageFileInfo(
                    detectedFormat.Value.ContentType,
                    detectedFormat.Value.Extension,
                    null,
                    null,
                    null);
            }

            var imageInfo = await Image.IdentifyAsync(stream, cancellationToken);
            if (imageInfo.Width <= 0 || imageInfo.Height <= 0)
            {
                return null;
            }

            var pixelCount = checked((long)imageInfo.Width * imageInfo.Height);

            if (imageInfo.Width > _options.MaxWidth ||
                imageInfo.Height > _options.MaxHeight ||
                pixelCount > _options.MaxPixelCount)
            {
                return new ImageFileInfo(
                    detectedFormat.Value.ContentType,
                    detectedFormat.Value.Extension,
                    imageInfo.Width,
                    imageInfo.Height,
                    "Görsel çözünürlüğü izin verilen sınırı aşıyor.");
            }

            stream.Position = originalPosition;
            using var decodedImage = await Image.LoadAsync(stream, cancellationToken);

            if (decodedImage.Width != imageInfo.Width || decodedImage.Height != imageInfo.Height)
            {
                return null;
            }

            return new ImageFileInfo(
                detectedFormat.Value.ContentType,
                detectedFormat.Value.Extension,
                imageInfo.Width,
                imageInfo.Height,
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static async Task<DetectedImageFormat?> DetectFormatAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[32];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new DetectedImageFormat("image/jpeg", "jpg");
        }

        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return new DetectedImageFormat("image/png", "png");
        }

        if (read >= 12 &&
            Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
            Encoding.ASCII.GetString(header, 8, 4) == "WEBP")
        {
            return new DetectedImageFormat("image/webp", "webp");
        }

        if (read >= 12 && Encoding.ASCII.GetString(header, 4, 4) == "ftyp")
        {
            var brand = Encoding.ASCII.GetString(header, 8, 4);
            if (brand is "heic" or "heix" or "hevc" or "hevx" or "mif1" or "msf1")
            {
                return new DetectedImageFormat("image/heic", "heic");
            }
        }

        return null;
    }

    private readonly record struct DetectedImageFormat(string ContentType, string Extension);
}

public sealed record ImageFileInfo(
    string ContentType,
    string Extension,
    int? Width,
    int? Height,
    string? ValidationError);