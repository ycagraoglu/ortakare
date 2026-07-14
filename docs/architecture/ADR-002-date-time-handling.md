# ADR-002 — Date and Time Handling

- **Status:** Accepted
- **Scope:** Ortakare API and PWA
- **Audience:** Backend team, frontend team, Codex and AI coding agents

## Context

Ortakare kullanıcıları farklı saat dilimlerinde bulunabilir. Etkinlik tarihi, oluşturulma zamanı, güncellenme zamanı, token süresi ve background job zamanları aynı anı tutarlı biçimde temsil etmelidir.

Sunucu veya veritabanı saat dilimine bağlı tarih saklamak; kullanıcı arayüzünde yanlış saat gösterilmesine, yaz/kış saati problemlerine ve farklı bölgelerde tutarsız davranışlara neden olabilir.

## Accepted Decision

Tüm sistem zamanları backend ve veritabanında UTC olarak tutulacaktır.

API tarih-saat alanlarını ISO 8601 UTC formatında döndürecektir:

```text
2027-03-10T18:00:00Z
```

Alan adları mümkün olduğunda UTC anlamını açıkça gösterecektir:

```text
EventDateUtc
CreatedAtUtc
UpdatedAtUtc
ExpiresAtUtc
RevokedAtUtc
```

PWA, API'den aldığı UTC tarihleri kullanıcıya gösterirken kullanıcının cihazındaki veya seçtiği IANA saat dilimindeki local zamana çevirecektir.

Örnek:

```text
API: 2027-03-10T18:00:00Z
Türkiye UI: 10 Mart 2027 21:00
```

UI tarih girişlerinde kullanıcı local tarih ve saat seçer. Frontend bu değeri API'ye göndermeden önce UTC'ye çevirir.

```text
Kullanıcı seçimi: 10 Mart 2027 21:00 Europe/Istanbul
API request:    2027-03-10T18:00:00Z
```

## Backend Rules

- PostgreSQL tarafında zaman bilgileri `timestamp with time zone` olarak saklanır.
- Backend yeni zaman üretirken `DateTime.Now` kullanmaz.
- Uygulama kodunda `TimeProvider` tercih edilir.
- API'ye zaman noktası olarak gelen `DateTime` değerleri UTC olmak zorundadır.
- UTC olmayan veya `DateTimeKind.Unspecified` değerler doğrulama sırasında reddedilir.
- Token süreleri, cleanup işlemleri ve Hangfire job kontrolleri UTC üzerinden hesaplanır.
- Backend kullanıcıya özel local saat dönüşümü yapmaz.
- Sunucunun işletim sistemi saat dilimine güvenilmez.

## Frontend Rules

- API'den gelen `Z` veya UTC offset içeren değerler gerçek bir zaman noktası olarak parse edilir.
- UI gösterimi sırasında kullanıcının local saat dilimine çevrilir.
- Tarih formatlama tek bir ortak yardımcı üzerinden yapılır; component'lerde dağınık `new Date(...).toLocaleString(...)` kullanımı tekrar edilmez.
- Formlardaki local tarih-saat, request hazırlanırken UTC ISO string'e dönüştürülür.
- API'den gelen UTC değer uygulama state'inde mümkün olduğunca değiştirilmeden korunur.
- Kullanıcının seçtiği saat dilimi özelliği eklenirse IANA adı kullanılmalıdır; örneğin `Europe/Istanbul`.
- Sabit `+03:00` gibi offset değerleri kalıcı saat dilimi olarak saklanmaz.

## Date-Only Values

Doğum günü veya yalnızca takvim günü ifade eden değerler zaman noktası değildir. Böyle alanlar gerektiğinde `DateOnly` ve PostgreSQL `date` olarak modellenmelidir.

Bir `DateOnly` değeri gereksiz biçimde UTC'ye çevrilmemelidir.

## Codex Rules

Codex:

- Backend'e `DateTime.Now` eklemez.
- Zaman noktalarını UTC olarak üretir, saklar ve döndürür.
- UI'da gösterilen UTC değerleri local zamana dönüştürür.
- Local UI input'unu API request öncesinde UTC'ye çevirir.
- Sabit Türkiye offset'i gömmez; saat dilimi dönüşümünü platform API'si veya güvenilir date-time yardımcılarıyla yapar.
- Tarih alanının zaman noktası mı yoksa yalnızca takvim tarihi mi olduğunu ayırt eder.
- Tarih dönüşümü için ortak frontend utility oluşturur ve tekrar eden bilgiyi merkezileştirir.

## Testing Priorities

- UTC API değeri Türkiye local saatinde doğru gösterilir.
- Local form değeri doğru UTC değerine çevrilir.
- UTC olmayan backend request reddedilir.
- Yaz/kış saati uygulayan bir saat diliminde dönüşüm doğru çalışır.
- Date-only alanlarda gün kayması oluşmaz.

## Consequences

### Positive

- Kullanıcılar kendi yerel saatlerinde doğru değerleri görür.
- Sunucu lokasyonundan bağımsız tutarlı davranış elde edilir.
- Global kullanım ve farklı saat dilimleri desteklenir.
- Token, job ve event zamanı hesapları öngörülebilir olur.

### Accepted Trade-off

Frontend her gösterim ve form gönderiminde açık bir UTC/local dönüşümü yapmak zorundadır.

## Final Binding Rule

> Backend ve veritabanı zamanı UTC olarak saklar ve döndürür. Kullanıcı arayüzü UTC değerleri kullanıcının yerel saat dilimine çevirerek gösterir; local kullanıcı girişlerini API'ye göndermeden önce UTC'ye dönüştürür.
