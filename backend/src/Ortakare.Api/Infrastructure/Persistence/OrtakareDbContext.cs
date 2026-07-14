using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Auth.RefreshTokens;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Users;

namespace Ortakare.Api.Infrastructure.Persistence;

public sealed class OrtakareDbContext(DbContextOptions<OrtakareDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrtakareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
