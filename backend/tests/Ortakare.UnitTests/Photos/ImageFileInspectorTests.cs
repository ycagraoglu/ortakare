using Microsoft.Extensions.Options;
using Ortakare.Api.Features.Photos.UploadPhoto;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ortakare.UnitTests.Photos;

public sealed class ImageFileInspectorTests
{
    [Fact]
    public async Task InspectAsync_accepts_valid_png_and_returns_dimensions()
    {
        var inspector = CreateInspector();
        await using var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(32, 24))
        {
            await image.SaveAsPngAsync(stream);
        }

        stream.Position = 0;
        var result = await inspector.InspectAsync(stream, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(32, result.Width);
        Assert.Equal(24, result.Height);
        Assert.Null(result.ValidationError);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task InspectAsync_rejects_spoofed_png_header()
    {
        var inspector = CreateInspector();
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        };
        await using var stream = new MemoryStream(bytes);

        var result = await inspector.InspectAsync(stream, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task InspectAsync_rejects_image_exceeding_dimension_limit()
    {
        var inspector = CreateInspector(new PhotoUploadOptions
        {
            MaxWidth = 10,
            MaxHeight = 10,
            MaxPixelCount = 100
        });
        await using var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(20, 5))
        {
            await image.SaveAsPngAsync(stream);
        }

        stream.Position = 0;
        var result = await inspector.InspectAsync(stream, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.ValidationError);
        Assert.Equal(20, result.Width);
        Assert.Equal(5, result.Height);
    }

    private static ImageFileInspector CreateInspector(PhotoUploadOptions? options = null) =>
        new(Options.Create(options ?? new PhotoUploadOptions()));
}