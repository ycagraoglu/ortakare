using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Photos.DomainEvents;

public sealed record PhotoUploadedDomainEvent(
    Guid EventId,
    Guid OwnerUserId,
    Guid ParticipantId,
    Guid PhotoId,
    long FileSizeBytes,
    DateTime OccurredAtUtc) : IDomainEvent;
