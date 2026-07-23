using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ortakare.Api.Infrastructure.Persistence;

public sealed class OrtakareDbContextFactory : IDesignTimeDbContextFactory<OrtakareDbContext>
{
    public OrtakareDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required for EF Core design-time operations.");

        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrtakareDbContext(options);
    }
}
