# 012 — Production Readiness Review

## 1. Amaç

Bu belge, Ortakare backend uygulamasının production ortamına alınmadan önceki son teknik değerlendirmesidir.

Kapsam:

- ASP.NET Core 10 Web API
- PostgreSQL
- Entity Framework Core
- Cloudflare R2 object storage
- Hangfire background jobs
- Outbox ve domain-event akışı
- SSE bildirim akışı
- Health checks
- OpenTelemetry
- Unit ve integration testleri
- IIS üzerinde tek instance deployment

Bu rapor yalnızca kodun yazılmış olmasını değil; migration güvenliğini, geri dönüş planını, secret yönetimini, operasyon görünürlüğünü ve production doğrulamasını değerlendirir.

---

## 2. Genel karar

**Karar: ŞARTLI NO-GO**

Kod tabanındaki production hardening çalışmaları önemli ölçüde tamamlandı. Ancak aşağıdaki blocker maddeler kapatılmadan production yayını önerilmez:

1. Backend CI başarılı şekilde tamamlanmalı.
2. Tüm solution için `dotnet restore`, `dotnet build -c Release --warnaserror` ve `dotnet test -c Release` başarılı olmalı.
3. EF Core model snapshot güncel modelle yeniden üretilmeli ve temiz PostgreSQL veritabanına migration zinciri uygulanmalı.
4. Production secret ve configuration değerleri doğrulanmalı.
5. PostgreSQL backup ve rollback prosedürü canlıya çıkmadan önce denenmeli.

Bu koşullar sağlandığında karar **GO** olarak güncellenebilir.

---

## 3. Tamamlanan production hardening çalışmaları

| No | Çalışma | Durum |
|---|---|---|
| 001 | Gallery export download security | Tamamlandı |
| 002 | Duplicate active export protection | Tamamlandı |
| 003 | Gallery export expiration | Tamamlandı |
| 004 | Expired export cleanup | Tamamlandı |
| 005 | Orphan object cleanup | Tamamlandı |
| 006 | Upload security hardening | Kodlandı; build/test doğrulaması gerekli |
| 007 | Outbox locking | Kodlandı; PostgreSQL concurrency doğrulaması gerekli |
| 008 | PostgreSQL index audit | Kodlandı; query-plan doğrulaması gerekli |
| 009 | Health checks hardening | Kodlandı |
| 010 | Observability | Kodlandı; OTLP export doğrulaması gerekli |
| 011 | Integration tests hardening | Testler eklendi; test çalıştırması gerekli |
| 012 | Production readiness review | Bu belge |

---

## 4. Blocker riskler

### B-01 — CI başarısız

Son gözlenen Backend CI çalışması başarısızdır. Runner smoke-test job'u başarısız olmuş, buna bağlı olarak build ve test job'u çalıştırılmadan atlanmıştır.

Bu nedenle aşağıdakiler doğrulanmış kabul edilemez:

- NuGet restore
- Release build
- warnings-as-errors
- unit tests
- integration tests
- SQLite outbox concurrency testi
- OpenTelemetry paket uyumluluğu

**Kapatma kriteri:**

```bash
dotnet restore backend/Ortakare.slnx
dotnet build backend/Ortakare.slnx -c Release --no-restore --warnaserror
dotnet test backend/Ortakare.slnx -c Release --no-build
```

Komutların tamamı exit code `0` ile bitmelidir.

### B-02 — EF Core snapshot güncel modelin tamamını temsil etmiyor

Mevcut snapshot içinde bazı aktif entity ve güncel index tanımları eksik veya eski görünmektedir. Özellikle bildirim ve audit modelleri ile son index değişikliklerinin snapshot ile model arasında uyumsuzluk riski vardır.

Bu durum sonraki `dotnet ef migrations add` işleminde beklenmeyen tablo veya index değişiklikleri üretebilir.

**Kapatma kriteri:**

1. Geçici, temiz bir PostgreSQL veritabanı oluştur.
2. Mevcut migration zincirini uygula.
3. Model snapshot'ı EF CLI ile yeniden üret.
4. Boş bir doğrulama migration'ı üret:

```bash
dotnet ef migrations add VerifyModelConsistency \
  --project backend/src/Ortakare.Api \
  --startup-project backend/src/Ortakare.Api
```

Bu migration anlamlı schema değişikliği üretmemelidir. Üretiyorsa snapshot/model uyuşmazlığı devam ediyor demektir.

### B-03 — Migration zinciri temiz PostgreSQL üzerinde denenmedi

Migration dosyaları yazılmış olsa da production ile aynı PostgreSQL major sürümünde sıfırdan uygulanması doğrulanmamıştır.

**Kapatma kriteri:**

```bash
dotnet ef database update \
  --project backend/src/Ortakare.Api \
  --startup-project backend/src/Ortakare.Api
```

Ardından aşağıdakiler doğrulanmalıdır:

- Bütün tablolar oluşuyor.
- Partial unique index oluşuyor.
- Notification partial indexleri oluşuyor.
- Outbox lock kolonları ve indexleri oluşuyor.
- Gallery export expiration alanları oluşuyor.
- Migration history beklenen sırada oluşuyor.

### B-04 — Production secrets doğrulanmadı

Production ortamında aşağıdaki değerler kaynak kod veya repository içinde tutulmamalıdır:

- PostgreSQL connection string
- JWT signing key
- R2 access key
- R2 secret key
- Hangfire dashboard username/password
- OTLP exporter credential/header bilgileri

**Kapatma kriteri:**

- Secrets IIS environment variables, Windows secret store veya ayrı bir secret-management sistemi üzerinden sağlanmalı.
- JWT signing key en az 32 karakter olmalı; production için yüksek entropili ve benzersiz olmalı.
- Development ve production secret değerleri kesinlikle aynı olmamalı.
- R2 credential yalnızca gerekli bucket yetkilerine sahip olmalı.

### B-05 — Backup ve rollback denemesi yapılmadı

Kod rollback yapılabilse bile forward-only migration sonrası eski uygulama schema ile uyumsuz olabilir.

**Kapatma kriteri:**

- Deployment öncesi PostgreSQL backup alınmalı.
- Backup restore işlemi test ortamında denenmeli.
- R2 lifecycle ve silme işlemleri için geri dönüş sınırı kabul edilmeli.
- Release paketi ve önceki çalışan release paketi aynı sunucuda saklanmalı.

---

## 5. High riskler

### H-01 — Outbox locking gerçek PostgreSQL concurrency altında doğrulanmadı

SQLite ilişkisel test faydalıdır ancak PostgreSQL locking ve transaction davranışının birebir karşılığı değildir.

Production öncesi test:

- Aynı anda 5–10 processor başlat.
- En az 1.000 outbox mesajı oluştur.
- Her mesajın delivery count değerinin beklenen at-least-once sınırlarında olduğunu kontrol et.
- Worker process'i lock aldıktan sonra sonlandır.
- Lock timeout sonrası mesajın tekrar claim edilebildiğini doğrula.

Not: Tasarım `at-least-once` delivery sağlar. Delivery channel'ları idempotent olmalıdır.

### H-02 — Upload pipeline build ve kötü niyetli dosyalarla doğrulanmadı

Doğrulanması gereken örnekler:

- Uzantısı `.jpg` olup içeriği farklı format olan dosya
- Truncated JPEG/PNG/WEBP
- Çok büyük dimension ve pixel-count içeren dosya
- Bozuk EXIF metadata
- Yanlış MIME type
- Sıfır byte dosya
- Maksimum boyut sınırının hemen altı ve üstü

### H-03 — Cleanup job'ları production dry-run moduna sahip değil

Object storage silme operasyonları geri döndürülemez olabilir.

İlk production çalıştırmasında öneri:

- Küçük batch size kullan.
- Grace period değerini yüksek tut.
- İlk çalıştırmadan önce aday object key listesini loglardan gözden geçir.
- R2 versioning veya alternatif kurtarma mekanizması varsa aktif et.

### H-04 — Health readiness dış bağımlılıklara bağlı

R2 veya PostgreSQL kısa süreli sorun yaşarsa readiness `503` döner. IIS tek instance mimarisinde bunun ne şekilde yorumlanacağı netleştirilmelidir.

- Liveness yalnızca process kontrolü için kullanılmalı.
- Readiness load balancer veya dış monitoring için kullanılmalı.
- IIS process restart kararı readiness sonucuna bağlanmamalı.

---

## 6. Medium riskler

### M-01 — OTLP collector doğrulanmadı

Kontrol edilmesi gerekenler:

- OTLP endpoint erişimi
- TLS ve authentication header ayarları
- Trace sampling oranı
- Npgsql span üretimi
- Custom outbox metriclerinin collector'a ulaşması
- Hassas payload taşınmadığı

### M-02 — PostgreSQL indexleri gerçek veri ile analiz edilmedi

Migration sonrası gerçek veya production-benzeri veri üzerinde:

```sql
EXPLAIN (ANALYZE, BUFFERS)
```

çalıştırılmalıdır.

Özellikle:

- Event owner listesi
- Event photo listesi
- Notification cursor pagination
- Unread notification count
- Gallery export cleanup
- Outbox claim sorgusu

### M-03 — Offset pagination büyüyen tablolarda maliyetli olabilir

Event ve photo listelerinde yüksek sayfalara çıkıldığında `Skip/Take` maliyeti artacaktır.

V1 için kabul edilebilir; veri hacmi büyüdüğünde cursor pagination'a geçilmelidir.

### M-04 — Tek instance operasyon riski

V1 tek instance olduğundan:

- Deployment sırasında kısa kesinti olabilir.
- Uygulama process'i kapanırsa SSE bağlantıları kopar.
- Background job ve API aynı process kaynaklarını paylaşır.

Kabul edilebilir V1 sınırı olarak belgelenmelidir.

---

## 7. Production configuration checklist

### ASP.NET Core / IIS

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] HTTPS binding aktif
- [ ] HSTS aktif
- [ ] Forwarded headers yalnızca güvenilir proxy'lerden kabul ediliyor
- [ ] stdout log geçici troubleshooting dışında kapalı veya sınırlandırılmış
- [ ] App Pool identity klasör izinleri minimum seviyede
- [ ] App Pool start mode ve idle timeout ihtiyaca göre ayarlı
- [ ] Web garden kapalı; tek worker process

### PostgreSQL

- [ ] SSL bağlantısı etkin
- [ ] Uygulama kullanıcısı superuser değil
- [ ] Connection pool boyutu sunucu kapasitesine uygun
- [ ] Statement timeout tanımlı
- [ ] Backup takvimi aktif
- [ ] Restore testi yapılmış
- [ ] Migration uygulama hesabının yetkileri kontrollü

### Cloudflare R2

- [ ] Credential yalnızca gerekli bucket'a erişiyor
- [ ] CORS yalnızca izin verilen originleri içeriyor
- [ ] Signed URL ömrü doğrulandı
- [ ] Production bucket development'tan ayrı
- [ ] Cleanup prefixleri doğru
- [ ] Object lifecycle politikaları gözden geçirildi

### JWT ve authentication

- [ ] Production signing key benzersiz
- [ ] Issuer doğru
- [ ] Audience doğru
- [ ] Access-token süresi kabul edildi
- [ ] Refresh-token revoke senaryosu test edildi
- [ ] Sunucu saat senkronizasyonu aktif

### Hangfire

- [ ] Dashboard production'da kapalı veya güçlü kimlik doğrulamalı
- [ ] Dashboard public internet'e doğrudan açık değil
- [ ] Worker count PostgreSQL ve CPU kapasitesine uygun
- [ ] Shutdown timeout değerleri IIS recycle süresine uygun
- [ ] Retry davranışları gözden geçirildi

### Observability

- [ ] OTLP endpoint doğrulandı
- [ ] Trace sampling oranı belirlendi
- [ ] Error rate alarmı tanımlandı
- [ ] HTTP latency alarmı tanımlandı
- [ ] Outbox failure alarmı tanımlandı
- [ ] Readiness failure alarmı tanımlandı
- [ ] Disk, CPU ve memory monitoring aktif

---

## 8. Deployment planı

### T-24 saat

1. Release candidate commit'i dondur.
2. CI'yi başarıyla tamamla.
3. Temiz PostgreSQL üzerinde migration zincirini çalıştır.
4. Test ortamında smoke test çalıştır.
5. Production backup doğrulamasını yap.

### T-30 dakika

1. Kullanıcı trafiğinin düşük olduğu pencereyi seç.
2. PostgreSQL backup al.
3. Mevcut uygulama paketini arşivle.
4. Hangfire job durumlarını kontrol et.
5. Outbox backlog sayısını kaydet.

### Deployment

1. Uygulamayı kontrollü olarak durdur.
2. Migration'ları ayrı deployment adımı olarak çalıştır.
3. Yeni release paketini yayınla.
4. App Pool'u başlat.
5. `/health/live` kontrolü yap.
6. `/health/ready` kontrolü yap.
7. Authentication smoke test yap.
8. Event create/read smoke test yap.
9. Test event'ine küçük bir görsel yükle.
10. Gallery export oluştur ve download URL doğrula.
11. Hangfire ve outbox akışını kontrol et.

### İlk 30 dakika

- HTTP 5xx oranını izle.
- PostgreSQL connection sayısını izle.
- Outbox retry ve failure sayılarını izle.
- R2 hata oranını izle.
- Memory ve CPU kullanımını izle.
- Cleanup job'larını hemen yüksek batch ile çalıştırma.

---

## 9. Rollback planı

### Uygulama hatası, schema uyumluysa

1. App Pool'u durdur.
2. Önceki release paketini geri yükle.
3. App Pool'u başlat.
4. Health ve smoke testleri çalıştır.

### Migration sonrası eski uygulama schema ile uyumsuzsa

Tercih edilen yöntem database migration `Down` çalıştırmak yerine:

1. Uygulamayı durdur.
2. Deployment öncesi PostgreSQL backup'ını restore et.
3. Önceki release paketini geri yükle.
4. R2 üzerinde deployment sırasında oluşan objectleri ayrıca değerlendir.

Production rollback sırasında destructive `Down` migration çalıştırılması varsayılan yöntem olmamalıdır.

---

## 10. Smoke test listesi

- [ ] `/health/live` → 200
- [ ] `/health/ready` → 200
- [ ] Yeni kullanıcı kaydı
- [ ] Login ve access token
- [ ] Refresh token
- [ ] Event create
- [ ] Event list
- [ ] Event detail
- [ ] Participant oluşturma/erişim
- [ ] Geçerli fotoğraf upload
- [ ] Geçersiz fotoğraf rejection
- [ ] Photo list
- [ ] Gallery export create
- [ ] Duplicate export → 409
- [ ] Completed export download URL
- [ ] Expired export → 410
- [ ] Notification list
- [ ] Unread notification count
- [ ] SSE stream bağlantısı
- [ ] Outbox mesajı işleniyor
- [ ] Hangfire job çalışıyor

---

## 11. GO kriterleri

Aşağıdakilerin tamamı sağlanırsa production yayını için **GO** kararı verilebilir:

- [ ] CI tamamen yeşil
- [ ] Release build warnings-as-errors ile başarılı
- [ ] Unit testler başarılı
- [ ] Integration testler başarılı
- [ ] Temiz PostgreSQL migration testi başarılı
- [ ] Snapshot/model consistency doğrulandı
- [ ] Production secrets tanımlandı
- [ ] Backup ve restore denendi
- [ ] `/health/live` ve `/health/ready` başarılı
- [ ] Smoke test listesi başarılı
- [ ] Rollback paketi hazır
- [ ] İlk yayın monitoring sorumlusu belirlendi

---

## 12. Nihai değerlendirme

Ortakare backend mimarisi V1 için güçlü bir production temel seviyesine ulaşmıştır. Download güvenliği, duplicate protection, expiration, cleanup, upload doğrulama, outbox locking, health checks, observability ve integration test katmanları tasarlanmıştır.

Ancak production readiness yalnızca kod kapsamıyla ölçülmez. Mevcut durumda eksik olan ana unsur **çalıştırılmış doğrulama kanıtıdır**. CI, gerçek PostgreSQL migration testi, snapshot tutarlılığı, backup/restore ve production secret doğrulamaları tamamlanmadan sistem production-ready kabul edilmemelidir.

Bu blocker'lar kapatıldıktan sonra kalan riskler V1 için yönetilebilir seviyededir.
