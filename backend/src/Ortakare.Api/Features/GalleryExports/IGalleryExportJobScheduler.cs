namespace Ortakare.Api.Features.GalleryExports;

public interface IGalleryExportJobScheduler
{
    string Enqueue(Guid exportId);
}