# 010 — Observability Hardening

## Amaç

Ortakare API için vendor-bağımsız trace ve metric üretmek; HTTP istekleri, dış HTTP çağrıları, PostgreSQL aktiviteleri, .NET runtime ve kritik uygulama akışlarını OTLP üzerinden gözlemlenebilir hale getirmek.

## Eklenen altyapı

- OpenTelemetry ASP.NET Core instrumentation
- HttpClient instrumentation
- .NET runtime metrics
- `Npgsql` ActivitySource dinleme
- Uygulamaya özel `Ortakare.Api` ActivitySource ve Meter
- OTLP exporter
- Parent-based ratio sampling

## Yapılandırma

```json
{
  "Observability": {
    "Enabled": true,
    "ServiceName": "ortakare-api",
    "OtlpEndpoint": "http://otel-collector:4317",
    "TraceSampleRatio": 0.1,
    "ExportSensitiveData": false
  }
}
```

`OtlpEndpoint` boş bırakılırsa exporter oluşturulmaz. Uygulama çalışmaya devam eder. Production ortamında collector kullanılması önerilir; API'nin doğrudan bir SaaS observability sağlayıcısına bağlanması yerine collector üzerinden gönderim yapılmalıdır.

## Üretilen custom metrikler

| Metrik | Tür | Açıklama |
|---|---|---|
| `ortakare.outbox.processed` | Counter | Başarıyla teslim edilen outbox mesajı |
| `ortakare.outbox.failed` | Counter | Teslimatı başarısız outbox mesajı |
| `ortakare.outbox.duration` | Histogram | Outbox batch işlem süresi |
| `ortakare.upload.accepted` | Counter | Kabul edilen upload |
| `ortakare.upload.rejected` | Counter | Reddedilen upload |
| `ortakare.upload.size` | Histogram | Upload dosya boyutu |
| `ortakare.background_job.succeeded` | Counter | Başarılı background job |
| `ortakare.background_job.failed` | Counter | Başarısız background job |
| `ortakare.background_job.duration` | Histogram | Background job çalışma süresi |

Bu aşamada outbox metrikleri üretim akışına bağlanmıştır. Upload ve background job instrumentları merkezi meter üzerinde tanımlanmış olup ilgili handler/job refactorlarında kullanılmaya hazırdır.

## Trace kapsamı

### HTTP

ASP.NET Core gelen istekleri otomatik trace eder. `/health/live` yüksek frekanslı probe gürültüsünü azaltmak için trace kapsamı dışındadır.

### PostgreSQL

`Npgsql` ActivitySource dinlenir. SQL parametre değerleri veya hassas payload bilgileri uygulama tarafından özel tag olarak eklenmez.

### Outbox

- `outbox.process_batch`
- `outbox.deliver`

span'leri oluşturulur. Tag'ler yalnızca mesaj kimliği, mesaj tipi, retry sayısı ve claim sayısı gibi operasyonel alanlardır. `PayloadJson` telemetry'ye eklenmez.

## Sampling

Varsayılan oran `0.1` yani yüzde 10'dur. Parent-based sampling kullanıldığı için upstream trace kararı korunur.

Öneri:

- Development: `1.0`
- Normal production: `0.05`–`0.20`
- Incident incelemesi: geçici olarak daha yüksek oran

Metric'ler trace sampling oranından etkilenmez.

## Güvenlik ve kardinalite

Telemetry tag'lerinde aşağıdaki alanlar kullanılmamalıdır:

- Email
- Telefon
- JWT
- Participant token
- Dosya adı
- Storage signed URL
- Outbox payload
- Exception içinde secret barındırabilecek ham request body

Kullanıcı veya event kimlikleri metric label olarak kullanılmamalıdır. Yüksek kardinaliteli kimlikler gerektiğinde yalnızca trace span üzerinde ve sınırlı şekilde değerlendirilmelidir.

## Collector örnek hedefleri

OTLP collector üzerinden aşağıdaki sistemlere yönlendirme yapılabilir:

- Grafana Tempo + Prometheus
- Jaeger
- Azure Monitor
- Datadog
- New Relic
- Honeycomb

Uygulama kodu exporter sağlayıcısından bağımsız kalır.

## Production doğrulaması

1. API'yi OTLP collector ile başlat.
2. Normal bir authenticated endpoint çağır.
3. Trace içinde ASP.NET Core request span'ini doğrula.
4. PostgreSQL child activity oluştuğunu doğrula.
5. Outbox mesajı üret ve `outbox.deliver` span'ini doğrula.
6. `ortakare.outbox.processed` metric artışını doğrula.
7. `/health/live` çağrılarının trace gürültüsü üretmediğini doğrula.
8. Telemetry backend içinde secret, token veya payload bulunmadığını kontrol et.

## Bilinen sınırlar

- GitHub ortamında restore/build çalıştırılamadığı için paket uyumluluğu branch üzerinde bağımsız olarak doğrulanmamıştır.
- Upload ve background job instrumentları tanımlanmıştır ancak bu aşamada bütün job sınıflarına tek tek bağlanmamıştır.
- Log export OpenTelemetry pipeline'ına dahil edilmemiştir; mevcut structured logging korunmuştur.
- Alert kuralları ve dashboard'lar deployment ortamında ayrıca tanımlanmalıdır.
