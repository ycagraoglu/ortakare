# 011 — Integration Tests Hardening

## Amaç

Production hardening kapsamında en yüksek riskli davranışların yalnızca handler seviyesinde değil, gerçek ASP.NET Core pipeline ve ilişkisel veritabanı davranışı üzerinden doğrulanması hedeflenmiştir.

## Eklenen testler

### Health endpointleri

`HealthEndpointTests` aşağıdaki davranışları kapsar:

- `GET /health/live` anonim erişilebilir.
- Liveness dependency kontrolü çalıştırmadan `200 OK` döner.
- Response `Cache-Control: no-store` içerir.
- Korumalı API endpointleri access token olmadan `401 Unauthorized` döner.

### Outbox concurrency

`OutboxConcurrencyTests` iki ayrı `OrtakareDbContext` ve iki ayrı `OutboxProcessor` örneğini aynı ilişkisel SQLite veritabanına karşı eşzamanlı çalıştırır.

Doğrulanan kurallar:

- Aynı outbox mesajı yalnızca bir kez teslim edilir.
- Mesaj başarıyla işlendiğinde `ProcessedAtUtc` doldurulur.
- İşlem tamamlandıktan sonra `LockId` ve `LockedAtUtc` temizlenir.
- Test, EF Core InMemory provider yerine gerçek ilişkisel `ExecuteUpdateAsync` davranışını kullanır.

## Neden SQLite kullanıldı?

Mevcut integration test factory hızlı endpoint testleri için EF Core InMemory provider kullanmaktadır. Ancak InMemory provider:

- ilişkisel transaction ve locking davranışını taklit etmez,
- SQL üretmez,
- `ExecuteUpdateAsync` gibi ilişkisel operasyonların gerçek davranışını doğrulamaz.

Bu nedenle outbox yarış koşulu testi ayrı bir SQLite veritabanı üzerinde çalışır. PostgreSQL'e özel sorgu planı ve `SKIP LOCKED` benzeri özelliklerin doğrulanması ayrıca gerçek PostgreSQL ortamında yapılmalıdır.

## Mevcut kritik kapsama

Repository içinde daha önce eklenmiş testlerle birlikte aşağıdaki alanlar kapsanmaktadır:

- gallery export duplicate protection,
- export metadata ve download-url ayrımı,
- expired export download reddi,
- expired export cleanup,
- orphan file cleanup,
- image signature ve decode kontrolleri,
- owner authorization sınırları,
- health liveness davranışı,
- outbox concurrent delivery koruması.

## Çalıştırma

```bash
dotnet test backend/tests/Ortakare.UnitTests/Ortakare.UnitTests.csproj -c Release
dotnet test backend/tests/Ortakare.IntegrationTests/Ortakare.IntegrationTests.csproj -c Release
```

## Production öncesi ek doğrulama

Aşağıdaki testler gerçek PostgreSQL ve gerçek veya staging R2 ortamında ayrıca çalıştırılmalıdır:

1. Aynı outbox mesajını 5–10 worker ile eşzamanlı claim etme.
2. Worker process'i delivery sonrasında fakat `SaveChanges` öncesinde sonlandırma.
3. PostgreSQL connection pool tükenmesi altında readiness davranışı.
4. R2 timeout, 403 ve bucket-not-found cevapları.
5. Büyük dosya, bozuk image payload ve MIME mismatch upload senaryoları.
6. Migration zincirinin boş PostgreSQL veritabanına baştan uygulanması.

## Bilinen sınırlar

- GitHub connector ortamında testler çalıştırılmamıştır.
- SQLite concurrency testi PostgreSQL'in tüm locking semantiğini birebir temsil etmez.
- Mevcut `OrtakareDbContextModelSnapshot` eksikleri giderilmeden migration tabanlı temiz PostgreSQL testleri güvenilir değildir.
