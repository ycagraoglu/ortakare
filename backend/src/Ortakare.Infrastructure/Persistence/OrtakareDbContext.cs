using Microsoft.EntityFrameworkCore;

namespace Ortakare.Infrastructure.Persistence;

public sealed class OrtakareDbContext(DbContextOptions<OrtakareDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrtakareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
