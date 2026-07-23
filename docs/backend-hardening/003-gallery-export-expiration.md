# 003 — Gallery Export Expiration

## Amaç

Gallery export ZIP dosyalarının süresiz biçimde object storage üzerinde tutulmasını önlemek ve indirme yaşam döngüsünü API seviyesinde açık hale getirmek.

## Karar

- Başarıyla tamamlanan her export, tamamlanma zamanından itibaren **7 gün** saklanır.
- `ExpiresAtUtc`, export `Completed` olarak kaydedilirken otomatik atanır.
- Mevcut completed kayıtlar migration sırasında `CompletedAtUtc + 7 gün` ile geriye dönük doldurulur.
- Süresi dolmuş export için yeni signed URL üretilmez.
- API `410 Gone` döndürür.
- Signed URL'nin kendi süresi hiçbir zaman export yaşam süresini aşamaz.
- Liste ve detay endpoint'leri signed URL üretmez; yalnızca `ExpiresAtUtc` ve `IsExpired` döndürür.

## Veri modeli

```text
GalleryExport
├── CompletedAtUtc
└── ExpiresAtUtc
```

Kural:

```text
ExpiresAtUtc = CompletedAtUtc + 7 gün
```

## API davranışı

### Export detayı

```http
GET /api/events/{eventId}/exports/{exportId}
```

Response alanları:

- `expiresAtUtc`
- `isExpired`

### Export listesi

```http
GET /api/events/{eventId}/exports
```

Her kayıt expiration bilgisini içerir; signed URL içermez.

### Download URL

```http
GET /api/events/{eventId}/exports/{exportId}/download-url
```

- Hazır değil: `409 Conflict`
- Kayıt bulunamadı: `404 Not Found`
- Süresi doldu: `410 Gone`
- Uygun: kısa ömürlü signed URL

## Veritabanı

Migration:

```text
20260718223000_AddGalleryExportExpiration
```

Cleanup sorgularını hızlandırmak için aşağıdaki index eklenmiştir:

```sql
CREATE INDEX "IX_GalleryExports_Status_ExpiresAtUtc"
ON "GalleryExports" ("Status", "ExpiresAtUtc");
```

## Güvenlik

- Süresi dolmuş bir storage key üzerinden yeni erişim yetkisi üretilemez.
- Signed URL süresi, export'un kalan ömrüyle sınırlandırılır.
- Listeleme ve polling işlemleri geçici erişim URL'si üretmez.

## Kabul kriterleri

- Completed export kaydedildiğinde expiration otomatik atanır.
- Existing expiration değeri yanlışlıkla üzerine yazılmaz.
- Pending, Processing, Failed ve Cancelled kayıtlar otomatik expire edilmez.
- Süresi dolan completed export için `410 Gone` döner.
- Detay ve liste response'ları expiration durumunu gösterir.
- Signed URL, export expiration zamanından daha ileri bir zamana geçerli olamaz.

## Sonraki adım

`004 — Gallery Export Cleanup Job` süresi dolmuş ZIP dosyalarını storage'dan silecek ve veritabanı kaydını güvenli biçimde temizleyecektir.
