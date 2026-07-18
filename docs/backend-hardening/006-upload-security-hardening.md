# 006 — Upload Security Hardening

## Problem

Dosya uzantısı ve istemcinin gönderdiği `Content-Type` güvenilir değildir. Saldırgan bir executable, bozuk dosya veya aşırı büyük çözünürlüklü görseli `.jpg` adıyla gönderebilir. Bu durum storage maliyeti, bellek tüketimi, decoder açıkları ve servis kesintisi riski oluşturur.

## Current State

Önceki uygulama JPEG, PNG, WebP ve HEIC için ilk baytları kontrol ediyordu. Bu kontrol format taklidini azaltıyordu ancak bozuk raster dosyaların tamamını doğrulamıyor ve görsel çözünürlüğünü sınırlamıyordu.

## Goals

- Dosya boyutunu storage işleminden önce kontrol etmek.
- Güvenilmeyen dosya adını normalize etmek ve sınırlandırmak.
- Magic number ile gerçek formatı belirlemek.
- JPEG, PNG ve WebP dosyalarını gerçekten decode ederek bozuk içeriği reddetmek.
- Genişlik, yükseklik ve toplam piksel sayısını sınırlamak.
- Bildirilen MIME ile tespit edilen MIME uyuşmazlığını reddetmek.
- Storage anahtarında kullanıcı dosya adını kullanmamak.
- DB kayıt hatasında yüklenen nesneyi geri silmek.

## Supported Formats

| Format | Magic number | Dimension check | Full decode check |
|---|---:|---:|---:|
| JPEG | Evet | Evet | Evet |
| PNG | Evet | Evet | Evet |
| WebP | Evet | Evet | Evet |
| HEIC | ISO-BMFF brand | Hayır | Hayır |

HEIC desteği geriye dönük uyumluluk için korunmuştur. ImageSharp HEIC decoder sağlamadığı için HEIC dosyaları V1'de magic number ve dosya boyutu ile doğrulanır. Production'ın ileri aşamasında HEIC dosyalarının izole bir worker üzerinde JPEG/WebP formatına dönüştürülmesi önerilir.

## Security Rules

Varsayılan sınırlar:

```json
{
  "PhotoUpload": {
    "MaxFileSizeBytes": 26214400,
    "MaxWidth": 12000,
    "MaxHeight": 12000,
    "MaxPixelCount": 100000000,
    "MaxOriginalFileNameLength": 255
  }
}
```

Dosya adı yalnızca metadata olarak tutulur. Storage anahtarı sunucu tarafından oluşturulur:

```text
events/{eventId}/participants/{participantId}/{clientUploadId}
```

## Validation Order

```text
Request file exists
→ File size
→ File name
→ Participant authorization
→ Idempotency lookup
→ Owner quota
→ Magic number
→ Dimensions
→ Full decode
→ MIME consistency
→ Object storage upload
→ Database save
```

## Error Responses

- `400 Bad Request`: boş dosya veya geçersiz dosya adı
- `413 Payload Too Large`: dosya boyutu limiti
- `415 Unsupported Media Type`: format, bozuk içerik veya MIME uyuşmazlığı
- `422 Unprocessable Entity`: çözünürlük veya piksel limiti

## Tests

- Geçerli PNG kabul edilir ve boyutları okunur.
- PNG imzası taklit eden bozuk içerik reddedilir.
- Boyut sınırını aşan görsel reddedilir.
- Inspector işlem sonunda stream konumunu başlangıca geri getirir.

## Acceptance Criteria

- İstemci MIME değeri tek başına güven kaynağı değildir.
- Bozuk JPEG, PNG ve WebP dosyaları storage'a ulaşmaz.
- Aşırı çözünürlüklü raster dosyaları storage'a ulaşmaz.
- Kullanıcı dosya adı storage key üretiminde kullanılmaz.
- Veritabanı kaydı başarısız olursa storage nesnesi silinmeye çalışılır.

## Future Improvements

- HEIC/HEIF decode ve güvenli transcoding worker'ı
- EXIF metadata stripping
- Antivirüs veya malware scanning pipeline
- Quarantine bucket
- Decoder işlemlerini ayrı process/container içinde çalıştırma
- Animated WebP frame ve decoded-memory limitleri
