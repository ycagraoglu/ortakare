# ADR-001 — Backend Architecture and Coding Principles

- **Status:** Accepted
- **Scope:** Ortakare Backend API
- **Target:** ASP.NET Core 10 LTS
- **Audience:** Development team, Codex and AI coding agents

## 1. Context

Ortakare is a standalone product. It must not be implemented as a StudioMan module or StudioMan API extension.

The product has its own repository, deployment lifecycle, database, Cloudflare R2 configuration, API and frontend. StudioMan-specific concepts such as `Tenant`, `Agreement`, `Quotation` and `SubscriptionEntitlement` must not leak into this codebase.

The backend must remain explicit, easy to debug, suitable for AI-assisted development and capable of growing without unnecessary architectural ceremony.

## 2. Accepted Decision

The backend will use:

- ASP.NET Core 10 LTS Web API.
- Controller-based HTTP endpoints.
- Vertical Slice Architecture.
- EF Core 10 with PostgreSQL.
- Direct `DbContext` usage in feature handlers where appropriate.
- Constructor injection for handler dependencies.
- FluentValidation with automatic request validation.
- Explicit dependency injection registration.
- Hangfire for long-running background work.
- Cloudflare R2 for object storage.
- SOLID, YAGNI and DRY as mandatory coding principles.

The backend will not use:

- MediatR.
- Minimal APIs.
- Generic Repository Pattern.
- A global CRUD-oriented service layer.
- Reflection-based request dispatch.
- A custom mediator that recreates MediatR.
- Speculative abstractions for hypothetical future requirements.

## 3. Backend Structure

```text
backend
├── Ortakare.sln
├── src
│   ├── Ortakare.Api
│   ├── Ortakare.Domain
│   └── Ortakare.Infrastructure
└── tests
    ├── Ortakare.UnitTests
    └── Ortakare.IntegrationTests
```

### Ortakare.Api

Contains application entry points and use-case implementations.

```text
Ortakare.Api
├── Features
├── Common
├── Authentication
├── Authorization
├── Middleware
├── Filters
├── Extensions
└── Program.cs
```

`Features` is the primary organization model. Application use cases must not be globally grouped under technical folders such as `Dtos`, `Handlers`, `Validators` and `Services`.

### Ortakare.Domain

Contains core domain concepts such as:

- User
- Event
- EventGuestParticipant
- EventGuestPhoto
- GalleryExport
- Package
- Payment

Domain entities must not depend on ASP.NET Core, controllers, HTTP models, Hangfire or Cloudflare R2 SDK types.

Do not introduce aggregate roots, domain events, specifications or complex DDD abstractions until a real business requirement justifies them.

### Ortakare.Infrastructure

Contains implementations for:

- Persistence
- Cloudflare R2
- Email
- Payments
- Hangfire jobs
- Caching
- Authentication

Meaningful external boundaries may use abstractions such as:

```csharp
IObjectStorageService
IEmailService
IPaymentService
ICurrentUser
IDateTimeProvider
```

An interface must represent a genuine boundary, behavioral contract or useful test seam. Do not create an interface automatically for every class.

## 4. Vertical Slice Rules

Code must be organized around business use cases.

```text
Features
├── Auth
│   ├── Register
│   ├── Login
│   └── RefreshToken
├── Events
│   ├── EventsController.cs
│   ├── CreateEvent
│   ├── GetEvent
│   ├── GetMyEvents
│   ├── UpdateEvent
│   └── CloseEvent
├── Participants
│   ├── PublicParticipantsController.cs
│   ├── JoinEvent
│   └── UpdateDisplayName
├── Photos
│   ├── PublicPhotosController.cs
│   ├── EventPhotosController.cs
│   ├── UploadPhoto
│   ├── DeleteGuestPhoto
│   ├── GetEventPhotos
│   └── DeleteOwnerPhoto
└── GalleryExports
    ├── GalleryExportsController.cs
    ├── CreateGalleryExport
    ├── GetGalleryExportStatus
    └── DownloadGalleryExport
```

A slice normally contains only the files required by that use case:

```text
CreateEvent
├── CreateEventRequest.cs
├── CreateEventResponse.cs
├── CreateEventValidator.cs
└── CreateEventHandler.cs
```

Do not create empty folders, marker interfaces, base handlers or shared abstractions solely to force every slice into an identical shape.

## 5. Controller Rules

Controllers are HTTP entry points only.

Controllers may:

- Define routes and HTTP attributes.
- Read route, query, header and body values.
- Call the relevant handler.
- Map application results to HTTP responses when required.

Controllers must not contain:

- EF Core queries.
- `DbContext` usage.
- Object storage operations.
- Payment logic.
- Business rules.
- Large mapping workflows.
- Multi-step application orchestration.

Handlers must be injected through the controller constructor.

```csharp
[ApiController]
[Route("api/events")]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    GetEventHandler getEventHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createEventHandler.HandleAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}
```

Action-level handler and validator injection is prohibited:

```csharp
public async Task<IActionResult> Create(
    [FromBody] CreateEventRequest request,
    [FromServices] CreateEventHandler handler,
    [FromServices] IValidator<CreateEventRequest> validator,
    CancellationToken cancellationToken)
```

## 6. Handler Rules

A handler represents one business use case.

Handlers:

- Expose an explicit `HandleAsync` method.
- Accept `CancellationToken` for asynchronous I/O.
- Do not return `IActionResult`.
- Do not depend on controllers.
- Do not read `HttpContext` directly when a dedicated abstraction is appropriate.
- Own the use-case orchestration and business rules of the slice.
- May directly use `OrtakareDbContext`.
- Must remain cohesive and use-case specific.

Do not introduce `IRequest`, `IRequestHandler`, `ISender`, `IMediator` or an internal custom mediator abstraction.

Replacing MediatR with a home-grown MediatR clone is prohibited.

## 7. EF Core and Persistence

EF Core is the relational persistence abstraction.

Handlers may directly use `OrtakareDbContext`.

The project must not implement generic repositories such as:

```csharp
IGenericRepository<TEntity>
Repository<TEntity>
GetAllAsync()
GetByIdAsync()
AddAsync()
UpdateAsync()
DeleteAsync()
```

Feature-specific query or persistence services may be introduced only for a real need, such as a complex reused query, specialized database operation or performance-sensitive implementation.

Read operations should use `AsNoTracking()` when tracking is not required. Queries should project only required response data where practical. Avoid premature `ToListAsync()` calls that load unnecessary rows before filtering or projection.

## 8. Validation

FluentValidation may be used. Standard request validation must run automatically before the handler executes.

- Required fields, lengths and basic formats belong to validators.
- Database-dependent and domain-dependent business rules belong to handlers or appropriate domain logic.
- Controllers and handlers must not manually invoke validators for standard request validation.

## 9. Public and Owner API Separation

Authenticated owner API examples:

```text
/api/events
/api/events/{eventId}
/api/events/{eventId}/photos
/api/events/{eventId}/exports
```

Anonymous guest API examples:

```text
/api/public/events/{galleryToken}
/api/public/events/{galleryToken}/participants
/api/public/events/{galleryToken}/photos
```

Public and authenticated operations must not be mixed into one oversized controller.

Knowing an event ID never grants access. Event ownership must be verified server-side. Participant tokens must be validated against the relevant event before guest photo operations are accepted.

## 10. SOLID

SOLID is mandatory and must be applied pragmatically.

### Single Responsibility

A class must have one clear reason to change.

- `UploadPhotoHandler` handles the upload use case.
- `R2ObjectStorageService` handles R2 operations.
- `GalleryExportJob` coordinates gallery export processing.

Do not create a `GuestGalleryService` containing participant, upload, deletion, export, payment, email and reporting logic.

### Open/Closed

Use abstractions at genuine external boundaries. Do not create strategy patterns or plugin systems for hypothetical providers.

### Liskov Substitution

Implementations of an abstraction must preserve its behavioral contract, including null, exception, retry and transaction behavior.

### Interface Segregation

Prefer small, cohesive interfaces. Avoid broad interfaces such as `IApplicationService` or `IGuestAlbumService` with unrelated methods.

### Dependency Inversion

Business use cases must not directly depend on Cloudflare R2, payment or email provider SDK details. Do not mechanically create interfaces for every internal class.

## 11. YAGNI

Do not build functionality because it may be useful later.

Without an approved requirement, do not introduce:

- MediatR or custom mediator.
- Event bus or message broker.
- Microservices.
- Kubernetes-oriented architecture.
- Generic repository.
- Specification Pattern.
- Domain-event infrastructure.
- Saga orchestration.
- Plugin system.
- Multi-storage-provider strategies.
- Chunk upload.
- Native-mobile-specific API behavior.
- StudioMan multi-tenant architecture.
- Agreement or quotation concepts.
- Premature shared NuGet packages with StudioMan.

The current requirement must drive the design. Future requirements may justify future refactoring.

## 12. DRY

Repeated business knowledge and repeated technical behavior must be centralized when the duplication represents the same rule.

Good reuse candidates:

- Gallery token validation.
- Participant token validation.
- Event ownership verification.
- Object storage key generation.
- Image validation rules.
- API result construction.
- Date/time abstraction.
- Signed URL configuration.

DRY does not mean similar-looking syntax must always share an abstraction.

Do not create `BaseHandler`, `BaseCrudService`, `UniversalMapper` or `CommonFeatureProcessor` merely to remove a few repeated lines.

> Remove duplicated knowledge, not merely duplicated syntax.

## 13. Error Handling

The API must use one consistent result contract. Unhandled exceptions must be processed by centralized exception middleware.

Controllers must not contain repetitive `try/catch` blocks.

Stack traces, SQL details, provider credentials, internal storage keys and sensitive exception details must never be returned to clients.

## 14. Hangfire

Hangfire is used for work that must not block an HTTP request, including:

- Gallery ZIP export.
- Export cleanup.
- Temporary-data cleanup.
- Asynchronous email orchestration where appropriate.

Jobs should receive stable identifiers such as `Guid exportId`.

Do not pass `DbContext`, streams, `IFormFile`, large DTO graphs or EF Core entities to jobs. Jobs must reload required state and be designed for retry and idempotency.

## 15. Cloudflare R2

Application use cases must not directly depend on Cloudflare or AWS SDK details.

Large photo and ZIP workflows must be stream-oriented. Do not load complete galleries or ZIP archives into API RAM.

Do not expose permanent public R2 URLs. Protected content must use short-lived signed URLs after authorization checks.

Storage keys must be generated centrally and must not rely on raw user-supplied file names.

## 16. Mandatory Codex Rules

Codex must not:

- Introduce MediatR or Minimal APIs.
- Add generic repositories.
- Use action-level `[FromServices]` handler injection.
- Put EF Core code in controllers.
- Create global business services containing unrelated use cases.
- Copy StudioMan tenant architecture.
- Create interfaces for every handler.
- Add speculative architecture.
- Refactor unrelated slices while implementing a scoped feature.
- Return raw exceptions to clients.

Codex must:

- Identify the relevant vertical slice first.
- Follow existing naming and folder conventions.
- Keep changes scoped to the requested use case.
- Use async APIs for I/O.
- Propagate `CancellationToken`.
- Use `AsNoTracking()` for read-only EF Core queries where appropriate.
- Validate ownership and authorization server-side.
- Treat public upload endpoints as hostile input surfaces.
- Add or update tests for changed business behavior.
- Prefer readable, explicit code over architectural cleverness.
- Apply SOLID, YAGNI and DRY pragmatically.

## 17. Testing Priorities

Initial integration tests should protect these scenarios:

- A user cannot access another user's event.
- A guest can join a valid public event.
- Invalid gallery tokens are rejected.
- A participant token cannot be used for another event.
- A valid participant can upload a valid photo.
- Invalid images are rejected.
- Duplicate client upload IDs do not create duplicate photo records.
- Guest photo deletion authorization is enforced.
- The event owner can list photos and request exports.
- Export retries do not create inconsistent state.
- Signed download access requires authorization.

Do not chase artificial 100% coverage. Tests must protect business behavior and critical boundaries.

## 18. Architectural Review Checklist

Before completing a feature, verify:

1. Is the code in the correct vertical slice?
2. Does the controller contain business logic?
3. Does the handler have one clear use-case responsibility?
4. Was an interface created without a real boundary?
5. Was a generic abstraction introduced for hypothetical reuse?
6. Is the same business rule duplicated?
7. Is similar syntax being over-abstracted?
8. Does the EF Core query retrieve unnecessary data?
9. Is ownership and authorization verified server-side?
10. Is `CancellationToken` propagated through asynchronous I/O?
11. Could a large file operation unnecessarily consume API RAM?
12. Does the implementation solve the current requirement?
13. Were relevant tests added or updated?

## 19. Final Binding Rule

> Build explicit vertical slices around real business use cases. Use ASP.NET Core Controllers and constructor-injected handlers without MediatR. Let EF Core handle relational persistence directly where appropriate. Abstract genuine external boundaries. Apply SOLID to keep responsibilities clear, YAGNI to prevent speculative architecture, and DRY to remove duplicated knowledge without creating artificial abstractions.

This ADR is binding unless superseded by a later accepted ADR.
