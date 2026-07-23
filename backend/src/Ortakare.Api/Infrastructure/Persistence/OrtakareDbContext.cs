using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Auth.RefreshTokens;
using Ortakare.Api.Features.EventAudit;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Outbox;

namespace Ortakare.Api.Infrastructure.Persistence;

public sealed class OrtakareDbContext(DbContextOptions<OrtakareDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventGuestParticipant> EventGuestParticipants => Set<EventGuestParticipant>();
    public DbSet<EventGuestPhoto> EventGuestPhotos => Set<EventGuestPhoto>();
    public DbSet<GalleryExport> GalleryExports => Set<GalleryExport>();
    public DbSet<EventAuditLog> EventAuditLogs => Set<EventAuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyGalleryExportLifecycleRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyGalleryExportLifecycleRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrtakareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyGalleryExportLifecycleRules()
    {
        foreach (var entry in ChangeTracker.Entries<GalleryExport>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.EnsureExpiration();
        }
    }
}
