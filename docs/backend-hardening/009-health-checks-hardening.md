# 009 - Health Checks Hardening

## Amaç

Uygulamanın yalnızca process olarak ayakta olup olmadığını, dış bağımlılıklarıyla birlikte trafik kabul etmeye hazır olup olmadığından ayırmak.

## Endpointler

### `GET /health/live`

Yalnızca ASP.NET Core process durumunu kontrol eder.

- PostgreSQL veya R2 kapalı olsa bile process çalışıyorsa `200 OK` döner.
- Load balancer veya process monitor tarafından liveness probe olarak kullanılmalıdır.
- Dependency hatalarında uygulamanın gereksiz yeniden başlatılmasını engeller.

### `GET /health/ready`

`ready` etiketi bulunan dependency kontrollerini çalıştırır.

Kontroller:

- PostgreSQL bağlantı açma ve `SELECT 1`
- Cloudflare R2 bucket üzerinde `ListObjectsV2` ve `MaxKeys = 1`

Bütün kontroller sağlıklıysa `200 OK`, en az biri başarısızsa `503 Service Unavailable` döner.

## Timeout

Her dependency kontrolü varsayılan olarak 3 saniyede zaman aşımına uğrar.

```json
{
  "HealthChecks": {
    "DependencyTimeoutSeconds": 3,
    "SlowCheckThresholdMilliseconds": 1000
  }
}
```

Ayarlar tanımlanmazsa güvenli varsayılanlar kullanılır.

## Response

```json
{
  "status": "Healthy",
  "totalDurationMilliseconds": 84.21,
  "timestampUtc": "2026-07-18T12:00:00Z",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "PostgreSQL bağlantısı ve sorgu yürütme başarılı.",
      "durationMilliseconds": 31.47,
      "data": {
        "elapsedMilliseconds": 29.92,
        "database": "ortakare"
      }
    }
  ]
}
```

Exception detayları response içine yazılmaz. Hassas connection string, access key ve secret key değerleri hiçbir health response içinde bulunmaz.

## Cache davranışı

Health endpointleri aşağıdaki header ile cevap verir:

```http
Cache-Control: no-store, no-cache
```

Proxy veya tarayıcının eski health sonucunu kullanması engellenir.

## IIS kullanımı

- Liveness: `/health/live`
- Readiness: `/health/ready`

Readiness endpointi deployment sonrasında trafik yönlendirilmeden önce kontrol edilmelidir.

## Bilinen sınırlar

- Uygulama başlangıcında migration otomatik uygulanmaz.
- Startup sırasında bucket yazma testi yapılmaz; readiness yalnızca düşük maliyetli listeleme işlemi kullanır.
- Health kontrolleri servis bağımlılıklarını doğrular, iş kurallarını veya uçtan uca kullanıcı senaryosunu doğrulamaz.
