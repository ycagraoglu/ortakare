namespace Ortakare.Api.Features.Events.UpdateEvent;

public sealed record UpdateEventRequest(
    string Title,
    DateTime EventDateUtc);