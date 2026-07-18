using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests;

public sealed class OrtakareApiFactory : WebApplicationFactory<Program>
{
    public TestObjectStorageService ObjectStorage { get; } = new();
    public TestGalleryExportJobScheduler GalleryExportJobs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false",
                ["GalleryExportCleanup:Enabled"] = "false",
                ["OrphanFileCleanup:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "https://pwa.ortakare.test",
                ["RateLimiting:AuthPermitLimit"] = "10000",
                ["RateLimiting:PublicPermitLimit"] = "10000",
                ["RateLimiting:UploadPermitLimit"] = "10000",
                ["RateLimiting:OwnerPermitLimit"] = "10000",
                ["RateLimiting:WindowSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrtakareDbContext>>();
            services.RemoveAll<OrtakareDbContext>();
            services.RemoveAll<IObjectStorageService>();
            services.RemoveAll<IGalleryExportJobScheduler>();

            services.AddDbContext<OrtakareDbContext>(options =>
                options.UseInMemoryDatabase($"ortakare-tests-{Guid.NewGuid():N}"));

            services.AddSingleton<IObjectStorageService>(ObjectStorage);
            services.AddSingleton<IGalleryExportJobScheduler>(GalleryExportJobs);
        });
    }
}