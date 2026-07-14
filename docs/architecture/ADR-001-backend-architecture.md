# ADR-001 — Backend Architecture and Coding Principles

- **Status:** Accepted
- **Scope:** Ortakare Backend API
- **Target:** ASP.NET Core 10 LTS
- **Audience:** Development team, Codex and AI coding agents

## 1. Context

Ortakare bağımsız bir üründür. StudioMan modülü veya StudioMan API uzantısı olarak geliştirilmeyecektir.

Backend; açık, kolay debug edilebilir, yapay zekâ destekli geliştirmeye uygun ve gereksiz mimari seremoni içermeyen bir yapıda olmalıdır.

## 2. Accepted Decision

Backend aşağıdaki kararlarla geliştirilecektir:

- ASP.NET Core 10 LTS Web API.
- Controller tabanlı HTTP endpoint'leri.
- Tek API projesi içinde Vertical Slice Architecture.
- EF Core 10 ve PostgreSQL.
- Handler'larda gerektiğinde doğrudan `OrtakareDbContext` kullanımı.
- Handler bağımlılıklarının controller constructor'ından inject edilmesi.
- FluentValidation ile otomatik request validation.
- Hangfire ile uzun süren background işlemler.
- Cloudflare R2 ile object storage.
- SOLID, YAGNI ve DRY prensiplerinin pragmatik uygulanması.

Aşağıdaki yapılar kullanılmayacaktır:

- MediatR veya özel yazılmış mediator.
- Minimal API.
- Generic Repository Pattern.
- Global CRUD service katmanı.
- Ayrı `Domain`, `Application` veya `Infrastructure` projeleri.
- Reflection tabanlı request dispatch.
- Gelecekte belki gerekir düşüncesiyle oluşturulan spekülatif soyutlamalar.

## 3. Backend Structure

```text
backend
├── Ortakare.slnx
├── Directory.Build.props
├── src
│   └── Ortakare.Api
│       ├── Features
│       ├── Common
│       ├── Infrastructure
│       │   ├── Persistence
│       │   ├── ObjectStorage
│       │   ├── Email
│       │   ├── Payments
│       │   └── BackgroundJobs
│       ├── Middleware
│       ├── Extensions
│       └── Program.cs
└── tests
    ├── Ortakare.UnitTests
    └── Ortakare.IntegrationTests
```

`Infrastructure`, ayrı bir proje veya Clean Architecture katmanı değildir. Tek `Ortakare.Api` projesindeki teknik uygulamaları gruplandıran klasördür.

Domain entity'leri ihtiyaç ortaya çıktıkça ilgili feature veya ortak persistence alanında oluşturulur. Boş proje, boş katman veya geleceğe dönük entity iskeleti oluşturulmaz.

## 4. Vertical Slice Rules

Kod teknik katmanlara göre değil iş kullanım senaryolarına göre düzenlenir.

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
│   └── UpdateEvent
├── Participants
│   ├── PublicParticipantsController.cs
│   ├── JoinEvent
│   └── UpdateDisplayName
├── Photos
│   ├── PublicPhotosController.cs
│   ├── EventPhotosController.cs
│   ├── UploadPhoto
│   ├── DeleteGuestPhoto
│   └── GetEventPhotos
└── GalleryExports
    ├── GalleryExportsController.cs
    ├── CreateGalleryExport
    ├── GetGalleryExportStatus
    └── DownloadGalleryExport
```

Bir slice yalnızca ihtiyaç duyduğu dosyaları içerir:

```text
CreateEvent
├── CreateEventRequest.cs
├── CreateEventResponse.cs
├── CreateEventValidator.cs
└── CreateEventHandler.cs
```

Her slice aynı görünsün diye boş klasör, marker interface, base handler veya gereksiz ortak abstraction oluşturulmaz.

## 5. Controller Rules

Controller yalnızca HTTP giriş noktasıdır.

Controller şunları yapabilir:

- Route ve HTTP attribute tanımlamak.
- Route, query, header ve body değerlerini okumak.
- İlgili handler'ı çağırmak.
- Sonucu HTTP response'a dönüştürmek.

Controller şunları yapamaz:

- EF Core sorgusu veya `DbContext` kullanımı.
- İş kuralı çalıştırmak.
- R2, ödeme veya e-posta operasyonu yapmak.
- Çok adımlı application workflow yönetmek.

Handler controller constructor'ından inject edilir:

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
        var result = await createEventHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
```

Action parametresinde `[FromServices]` ile handler veya validator inject edilmez.

## 6. Handler Rules

Her handler tek bir kullanım senaryosunu temsil eder.

- Açık bir `HandleAsync` metodu bulunur.
- I/O yapan metotlarda `CancellationToken` alınır ve iletilir.
- `IActionResult` döndürmez.
- Controller veya doğrudan `HttpContext` bağımlılığı taşımaz.
- İlgili iş kurallarını ve use-case orchestration'ını yürütür.
- Gerektiğinde `OrtakareDbContext` doğrudan kullanılabilir.
- Çok amaçlı global servis hâline gelmez.

`IRequest`, `IRequestHandler`, `ISender`, `IMediator` veya bunların özel yazılmış karşılıkları oluşturulmaz.

## 7. EF Core and Persistence

EF Core ilişkisel persistence abstraction'ıdır.

Generic repository oluşturulmaz:

```csharp
IGenericRepository<TEntity>
Repository<TEntity>
GetAllAsync()
GetByIdAsync()
AddAsync()
UpdateAsync()
DeleteAsync()
```

Read sorgularında tracking gerekmiyorsa `AsNoTracking()` kullanılır. Sorgular mümkün olduğunca yalnızca response için gerekli alanlara project edilir. Filtrelemeden önce gereksiz `ToListAsync()` çağrıları yapılmaz.

Feature-specific query service yalnızca gerçekten karmaşık veya birden çok slice tarafından tekrar kullanılan sorgular için oluşturulabilir.

## 8. Validation

FluentValidation otomatik çalışır.

- Zorunlu alan, uzunluk ve temel format kuralları validator'a aittir.
- Database ve domain bağımlı iş kuralları handler'a aittir.
- Controller veya handler standart validation için validator'ı manuel çağırmaz.

## 9. Public and Owner API Separation

Owner endpoint'leri authenticated'dır:

```text
/api/events
/api/events/{eventId}
/api/events/{eventId}/photos
/api/events/{eventId}/exports
```

Misafir endpoint'leri public'tir:

```text
/api/public/events/{galleryToken}
/api/public/events/{galleryToken}/participants
/api/public/events/{galleryToken}/photos
```

Event ID bilmek erişim hakkı vermez. Event ownership server tarafında doğrulanır. Participant token yalnızca bağlı olduğu event için geçerlidir.

## 10. SOLID

SOLID mekanik değil pragmatik uygulanır.

- **SRP:** Her handler ve teknik servis tek açık sorumluluğa sahiptir.
- **OCP:** Yalnızca gerçek dış sistem sınırlarında abstraction kullanılır.
- **LSP:** Aynı interface implementasyonları aynı davranış sözleşmesini korur.
- **ISP:** Büyük ve ilgisiz metotlar içeren servis interface'leri oluşturulmaz.
- **DIP:** Business use-case'ler R2, ödeme ve e-posta SDK detaylarına doğrudan bağımlı olmaz.

Her sınıfa interface oluşturmak SOLID değildir ve yapılmayacaktır.

## 11. YAGNI

Onaylanmış gereksinim olmadan aşağıdakiler eklenmez:

- MediatR veya custom mediator.
- Event bus veya message broker.
- Microservice altyapısı.
- Generic repository veya Specification Pattern.
- Domain event altyapısı.
- Saga veya plugin sistemi.
- Multi-storage-provider stratejisi.
- Chunk upload.
- StudioMan tenant, agreement veya quotation kavramları.
- StudioMan ile ortak NuGet paketi.

Bugünün kodu, yarının olası refactor'ından kaçınmak için gereksiz karmaşıklaştırılmaz.

## 12. DRY

Aynı iş bilgisi tekrar ediyorsa merkezileştirilir:

- Gallery token validation.
- Participant token validation.
- Event ownership doğrulaması.
- Storage key üretimi.
- Image validation kuralları.
- API result üretimi.
- Signed URL ayarları.

Benzer görünen birkaç satır için `BaseHandler`, `BaseCrudService`, `UniversalMapper` veya `CommonFeatureProcessor` oluşturulmaz.

> Tekrarlanan söz dizimini değil, tekrarlanan bilgiyi ortadan kaldır.

## 13. Error Handling

API tek bir result contract kullanır. Beklenmeyen hatalar merkezi exception middleware tarafından işlenir.

Controller'lara tekrarlanan `try/catch` blokları eklenmez. Stack trace, SQL detayı, provider credential veya internal storage key istemciye dönülmez.

## 14. Hangfire

Hangfire; ZIP export, export cleanup ve uzun süren e-posta işlemleri için kullanılır.

Job parametresi olarak stable ID gönderilir:

```csharp
Guid exportId
```

`DbContext`, stream, `IFormFile`, büyük DTO veya EF entity job parametresi yapılmaz. Job gerekli state'i tekrar yükler; retry ve idempotency dikkate alınır.

## 15. Cloudflare R2

Use-case'ler R2/AWS SDK detaylarına doğrudan bağımlı olmaz. Büyük fotoğraf ve ZIP işlemleri stream tabanlıdır. Galerinin tamamı RAM'e alınmaz.

Permanent public R2 URL kullanılmaz. Yetkilendirme sonrasında kısa süreli signed URL üretilir. Storage key ham kullanıcı dosya adına göre oluşturulmaz.

## 16. Mandatory Codex Rules

Codex:

- Önce ilgili vertical slice'ı belirler.
- Mevcut slice'ı genişletir; paralel mimari kurmaz.
- MediatR, Minimal API veya generic repository eklemez.
- Ayrı Domain, Application veya Infrastructure projesi oluşturmaz.
- Controller'a EF Core veya iş kuralı koymaz.
- Her handler için interface üretmez.
- Spekülatif abstraction eklemez.
- İlgisiz feature'ları refactor etmez.
- Async I/O ve `CancellationToken` kullanır.
- Ownership ve authorization kontrolünü server tarafında yapar.
- Public upload endpoint'lerini güvenilmeyen giriş yüzeyi kabul eder.
- Değişen davranış için test ekler veya günceller.
- Okunabilir açık kodu mimari sihre tercih eder.

## 17. Testing Priorities

Öncelikli integration testleri:

- Kullanıcı başka kullanıcının event'ine erişemez.
- Geçerli public event'e misafir katılabilir.
- Geçersiz gallery token reddedilir.
- Participant token başka event'te kullanılamaz.
- Geçerli participant fotoğraf yükleyebilir.
- Geçersiz görsel reddedilir.
- Aynı client upload ID duplicate kayıt oluşturmaz.
- Owner fotoğrafları listeleyebilir ve export isteyebilir.
- Export retry tutarsız state üretmez.
- Signed download için authorization gerekir.

Yapay bir yüzde yüz coverage hedeflenmez; testler kritik iş davranışlarını korur.

## 18. Final Binding Rule

> Gerçek iş kullanım senaryoları etrafında, tek ASP.NET Core API projesi içinde açık vertical slice'lar oluştur. Controller ve constructor-injected handler kullan; MediatR kullanma. EF Core'u gerektiğinde handler içinde doğrudan kullan. Yalnızca gerçek dış sistem sınırlarını soyutla. SOLID ile sorumlulukları net tut, YAGNI ile spekülatif mimariyi engelle, DRY ile yapay abstraction üretmeden tekrarlanan bilgiyi kaldır.

Bu ADR, daha sonraki kabul edilmiş bir ADR ile değiştirilmedikçe bağlayıcıdır.
