using System.Text;

namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class ImageFileInspector
{
    public async Task<ImageFileInfo?> InspectAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        var originalPosition = stream.Position;
        var header = new byte[32];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = originalPosition;

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new ImageFileInfo("image/jpeg", "jpg");
        }

        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return new ImageFileInfo("image/png", "png");
        }

        if (read >= 12 &&
            Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
            Encoding.ASCII.GetString(header, 8, 4) == "WEBP")
        {
            return new ImageFileInfo("image/webp", "webp");
        }

        if (read >= 12 && Encoding.ASCII.GetString(header, 4, 4) == "ftyp")
        {
            var brand = Encoding.ASCII.GetString(header, 8, 4);
            if (brand is "heic" or "heix" or "hevc" or "hevx" or "mif1" or "msf1")
            {
                return new ImageFileInfo("image/heic", "heic");
            }
        }

        return null;
    }
}

public sealed record ImageFileInfo(string ContentType, string Extension);