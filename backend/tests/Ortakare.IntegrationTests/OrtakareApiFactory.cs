using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests;

public sealed class OrtakareApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrtakareDbContext>>();
            services.RemoveAll<OrtakareDbContext>();

            services.AddDbContext<OrtakareDbContext>(options =>
                options.UseInMemoryDatabase($"ortakare-tests-{Guid.NewGuid():N}"));
        });
    }
}
