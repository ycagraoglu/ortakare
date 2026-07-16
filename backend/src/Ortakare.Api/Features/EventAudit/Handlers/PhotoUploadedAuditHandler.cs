using Ortakare.Api.Features.Photos.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class PhotoUploadedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<PhotoUploadedDomainEvent>
{
    public Task HandleAsync(
        PhotoUploadedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "PhotoUploaded",
            "Katılımcı etkinliğe yeni bir fotoğraf yükledi.",
            "Photo",
            domainEvent.PhotoId,
            new
            {
                domainEvent.ParticipantId,
                domainEvent.FileSizeBytes
            });

        return Task.CompletedTask;
    }
}
