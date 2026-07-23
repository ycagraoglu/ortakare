# 013 — Observability

## Amaç

Ortakare frontend uygulamasında kullanıcı deneyimi, API performansı ve beklenmeyen hatalar için vendor bağımsız, kişisel veri içermeyen ve correlation-id ile backend kayıtlarına bağlanabilen bir telemetry standardı oluşturmak.

## Mimari

Observability katmanı `frontend/src/shared/observability` altında merkezileştirilmiştir.

- `telemetry-types.ts`: event sözleşmeleri,
- `sanitize-telemetry.ts`: route, identifier ve hata mesajı temizleme,
- `telemetry.ts`: session context ve transport,
- `report-error.ts`: ErrorBoundary, global error ve rejected promise raporlama,
- `web-vitals.ts`: Core Web Vitals ölçümü.

Feature ve component kodları doğrudan vendor SDK'sına bağlanmaz. Telemetry sağlayıcısı ileride değiştirildiğinde yalnızca transport adapter'ı değiştirilmelidir.

## Telemetry event türleri

- `route-view`
- `api-request`
- `frontend-error`
- `web-vital`

Her event aşağıdaki ortak alanları taşır:

- temizlenmiş route,
- anonim session ID,
- UTC timestamp,
- opsiyonel release bilgisi.

Kullanıcı ID'si, e-posta, ad-soyad, token veya payload event context'ine eklenmez.

## PII ve secret politikası

Gönderilmesi yasak alanlar:

- access ve refresh token,
- Authorization header,
- participant/gallery token,
- request body,
- response body,
- form değerleri,
- e-posta ve telefon,
- tam query string,
- tam URL hash,
- dosya adı ve görsel içeriği.

Sanitizer aşağıdaki değerleri maskeler:

- GUID,
- uzun identifier/token benzeri segmentler,
- sayısal route ID'leri,
- e-posta adresleri,
- query string ve hash.

## Session kimliği

`ortakare.telemetry.session-id` anahtarı `sessionStorage` içinde rastgele UUID olarak tutulur. Bu değer kullanıcı hesabını temsil etmez ve browser oturumu kapandığında silinir.

## API ölçümü

Axios interceptor aşağıdaki alanları ölçer:

- HTTP method,
- sanitize edilmiş path,
- duration milliseconds,
- success/error/cancelled sonucu,
- status code,
- response correlation-id.

Payload ve header değerleri telemetry'ye yazılmaz. İptal edilen istekler hata event'i olarak gönderilmez; yalnızca `cancelled` API ölçümü oluşur.

401 refresh retry akışında başarısız ilk request ayrı kullanıcı hatası olarak raporlanmadan önce refresh denenir. Retry başarılı olursa final istek normal başarı metriği üretir.

## Correlation-id

Frontend her API isteğine `X-Correlation-Id` ekler. Backend response aynı header'ı döndürdüğünde API hata metriğine ve normalize edilmiş `ApiError` nesnesine eklenir.

Destek ekranında kullanıcıya correlation-id gösterilebilir. Backend log araması bu değer üzerinden yapılmalıdır.

## Global hata kaynakları

Aşağıdaki kaynaklar raporlanır:

- React ErrorBoundary,
- `window.error`,
- `unhandledrejection`,
- normalize edilmiş API hataları.

Stack trace production telemetry payload'ına eklenmez. Hata adı ve sanitize edilmiş, maksimum 500 karakterlik mesaj gönderilir.

## Web Vitals

Tarayıcı desteğine göre aşağıdaki metrikler PerformanceObserver ile toplanır:

- FCP,
- LCP,
- CLS,
- INP,
- TTFB.

Metric değerleri `good`, `needs-improvement` veya `poor` olarak sınıflandırılır. Bu çalışma üçüncü parti analytics SDK'sı eklemez.

## Transport

`VITE_TELEMETRY_URL` tanımlı değilse production telemetry gönderimi yapılmaz. Development ortamında event'ler yalnızca console debug seviyesinde görünür.

Production gönderim sırası:

1. `navigator.sendBeacon`,
2. beacon kabul edilmezse `fetch` + `keepalive` fallback.

Telemetry isteğinde credentials gönderilmez. Endpoint kendi auth gerektirmeyen, rate-limited ve küçük payload kabul eden bir ingest endpoint olmalıdır.

## Environment

```env
VITE_TELEMETRY_URL=https://api.example.com/api/telemetry/frontend
VITE_RELEASE=2026.07.22.1
```

`VITE_RELEASE` deploy edilen git SHA veya release number olmalıdır; secret içermemelidir.

## Backend gereksinimi

Frontend transport hazırdır ancak gerçek ingest endpoint'i ayrıca backend'de oluşturulmalıdır. Önerilen kurallar:

- anonymous POST,
- küçük body limiti,
- strict DTO validation,
- rate limiting,
- CORS allowlist,
- request body log redaction,
- event type allowlist,
- retention süresi,
- dashboards ve alert kuralları.

Endpoint hazır olmadan `VITE_TELEMETRY_URL` production ortamında tanımlanmamalıdır.

## Doğrulama kontrol listesi

- route event'lerinde query string bulunmuyor,
- API event'lerinde body/header bulunmuyor,
- email/GUID/token örnekleri maskeleniyor,
- correlation-id response üzerinden taşınıyor,
- cancelled request error alert üretmiyor,
- ErrorBoundary tek event üretiyor,
- Web Vitals desteklenen browser'da oluşuyor,
- sendBeacon başarısızlığında uygulama akışı etkilenmiyor,
- telemetry endpoint unavailable olduğunda kullanıcı işlemi bozulmuyor.

## Doğrulama durumu

Repository yazımları yapılmıştır. `npm ci`, typecheck, lint, test, production build, browser PerformanceObserver testi, gerçek telemetry ingest testi ve dashboard doğrulaması henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
