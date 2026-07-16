using Ortakare.Api.Features.Photos.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Notifications.Handlers;

public sealed class PhotoUploadedNotificationHandler(NotificationOutboxWriter writer)
    : IDomainEventHandler<PhotoUploadedDomainEvent>
{
    public Task HandleAsync(
        PhotoUploadedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        writer.AddOwnerNotification(
            domainEvent.OwnerUserId,
            domainEvent.EventId,
            "PhotoUploaded",
            "Yeni fotoğraf",
            "Etkinliğinize yeni bir fotoğraf yüklendi.",
            domainEvent.OccurredAtUtc,
            new
            {
                domainEvent.ParticipantId,
                domainEvent.PhotoId,
                domainEvent.FileSizeBytes
            });

        return Task.CompletedTask;
    }
}
