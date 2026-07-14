namespace Ortakare.Api.Features.PublicEvents.GetPublicEvent;

public sealed record GetPublicEventResponse(
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled);
