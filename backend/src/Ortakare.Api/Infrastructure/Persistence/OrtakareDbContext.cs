using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Users;

namespace Ortakare.Api.Infrastructure.Persistence;

public sealed class OrtakareDbContext(DbContextOptions<OrtakareDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrtakareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
