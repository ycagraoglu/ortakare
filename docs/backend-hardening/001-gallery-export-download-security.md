# 001 - Gallery Export Download Security

## Durum

Uygulandı. CI doğrulaması, GitHub Actions ödeme engeli nedeniyle bloklu durumdadır.

## Problem

Gallery export detay endpoint'i, export tamamlandığında signed download URL bilgisini otomatik üretiyor ve response içinde dönüyordu. Bu durum, yalnızca durum bilgisi görüntülemek isteyen istemciler için gereksiz URL üretimine ve indirme yetkisinin ayrı bir işlem olarak yönetilememesine neden oluyordu.

## Karar

Export durum sorgusu ile dosya indirme yetkisi birbirinden ayrıldı.

- `GET /api/events/{eventId}/exports/{exportId}` yalnızca export durumunu döndürür.
- `GET /api/events/{eventId}/exports/{exportId}/download-url` kısa ömürlü signed URL üretir.

## İş Kuralları

1. Export kaydı belirtilen event'e ait olmalıdır.
2. Event, oturum açmış owner kullanıcıya ait olmalıdır.
3. Başka kullanıcıya ait export için `404 Not Found` dönülür. Bu yaklaşım kaynak varlığının sızdırılmasını engeller.
4. Export durumu `Completed` değilse `409 Conflict` dönülür.
5. Completed export üzerinde `StorageKey` yoksa `404 Not Found` dönülür.
6. URL süresi `ObjectStorageOptions.SignedUrlMinutes` ayarı üzerinden belirlenir.
7. Storage key API response içinde açığa çıkarılmaz.

## Response

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "exportId": "00000000-0000-0000-0000-000000000000",
    "downloadUrl": "https://signed-storage-url",
    "expiresAtUtc": "2026-07-17T21:00:00Z"
  }
}
```

## Güvenlik Notları

- Endpoint JWT authentication gerektirir.
- Owner authorization sorgu seviyesinde uygulanır.
- Yetkisiz kaynak erişimleri `403` yerine `404` ile gizlenir.
- Signed URL yalnızca açık bir indirme isteğinde üretilir.
- URL kalıcı değildir ve object storage sağlayıcısı API istemcisinden gizlenir.

## Test Senaryoları

- Completed export için signed URL döner.
- Pending export için `409 Conflict` döner.
- Storage key bulunmayan completed export için `404 Not Found` döner.
- Başka owner'a ait export için `404 Not Found` döner.
- Export detay endpoint'i artık download URL döndürmez.

## Sonraki Adım

Gallery export expiration alanları ve cleanup job tasarlanacaktır. Expiration tamamlandığında download endpoint'ine `410 Gone` kontrolü eklenecektir.
