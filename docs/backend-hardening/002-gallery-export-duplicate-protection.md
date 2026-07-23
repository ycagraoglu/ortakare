# 002 — Gallery Export Duplicate Protection

## Problem

Aynı etkinlik için kullanıcı butona art arda basabilir, mobil istemci timeout sonrası isteği tekrar gönderebilir veya iki paralel HTTP isteği aynı anda işlenebilir. Yalnızca uygulama seviyesinde `AnyAsync` kontrolü yapmak yarış koşulunu tamamen engellemez.

## Decision

Bir etkinlik için aynı anda yalnızca bir adet aktif gallery export bulunabilir.

Aktif durumlar:

- `Pending`
- `Processing`

Terminal durumlar:

- `Completed`
- `Failed`
- `Cancelled`

Terminal duruma ulaşmış bir export, aynı etkinlik için yeni export oluşturulmasını engellemez.

## Protection Layers

### 1. Fast application check

Create handler önce aynı etkinlik için `Pending` veya `Processing` kayıt olup olmadığını sorgular. Aktif kayıt varsa `409 Conflict` döner.

Bu kontrol normal tekrar isteklerini hızlı ve anlaşılır biçimde karşılar.

### 2. PostgreSQL partial unique index

Paralel iki isteğin uygulama kontrolünü aynı anda geçebilmesi ihtimaline karşı veritabanında partial unique index kullanılır:

```sql
CREATE UNIQUE INDEX "UX_GalleryExports_EventId_Active"
ON "GalleryExports" ("EventId")
WHERE "Status" IN ('Pending', 'Processing');
```

Bu index yarış koşulunda ikinci insert işlemini kesin olarak reddeder.

### 3. Unique violation mapping

Handler yalnızca `UX_GalleryExports_EventId_Active` constraint ihlalini yakalar ve bunu beklenen `409 Conflict` sonucuna dönüştürür. Diğer database hataları yutulmaz.

## API Behaviour

```http
POST /api/events/{eventId}/exports
```

Aktif export yoksa:

- `202 Accepted`

Aktif export varsa:

- `409 Conflict`
- Mesaj: `Bu etkinlik için devam eden bir dışa aktarma zaten bulunuyor.`

## Why not only Idempotency-Key?

`Idempotency-Key` aynı istemci isteğinin tekrarını yönetmek için faydalıdır ancak farklı anahtarlarla veya anahtarsız gönderilen iki isteği engellemez. Buradaki iş kuralı doğrudan etkinlik bazında tek aktif export olduğu için veritabanı invariant'ı daha güçlü ve güvenlidir.

İleride genel amaçlı ödeme veya command işlemleri için ayrıca `Idempotency-Key` altyapısı eklenebilir.

## Acceptance Criteria

- Aynı etkinlik için ikinci `Pending` veya `Processing` export oluşturulamaz.
- Paralel isteklerde veritabanı en fazla bir aktif kayıt kabul eder.
- Önceki export `Completed`, `Failed` veya `Cancelled` ise yeni export oluşturulabilir.
- Başka etkinlikler birbirinden bağımsız export oluşturabilir.
- Beklenen unique violation `409 Conflict` olarak döner.
- Diğer database hataları normal exception pipeline'ına bırakılır.

## Deployment Note

Migration uygulanmadan önce mevcut veritabanında aynı etkinlik için birden fazla aktif export bulunmadığı doğrulanmalıdır:

```sql
SELECT "EventId", COUNT(*)
FROM "GalleryExports"
WHERE "Status" IN ('Pending', 'Processing')
GROUP BY "EventId"
HAVING COUNT(*) > 1;
```

Sonuç dönerse migration öncesinde hangi kaydın korunacağı operasyonel olarak belirlenmelidir. Migration mevcut kayıtları sessizce değiştirmez.

## Verification Status

Kod, migration, model snapshot ve integration testleri hazırlanmıştır. GitHub Actions ödeme/runner engeli nedeniyle otomatik build ve test doğrulaması şu anda blokludur.
