using Ortakare.Api.Features.GalleryExports;

namespace Ortakare.IntegrationTests;

public sealed class TestGalleryExportJobScheduler : IGalleryExportJobScheduler
{
    public List<Guid> ExportIds { get; } = [];

    public string Enqueue(Guid exportId)
    {
        ExportIds.Add(exportId);
        return $"test-job-{ExportIds.Count}";
    }

    public void Reset() => ExportIds.Clear();
}