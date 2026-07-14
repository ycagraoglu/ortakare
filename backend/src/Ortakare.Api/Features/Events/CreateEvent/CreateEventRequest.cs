namespace Ortakare.Api.Features.Events.CreateEvent;

public sealed record CreateEventRequest(
    string Title,
    DateTime EventDateUtc);
