# 008 — PostgreSQL Index Audit

## Amaç

Bu çalışma, mevcut endpoint sorgularını PostgreSQL B-tree index kurallarına göre inceleyerek sıralama, filtreleme ve storage referans kontrollerinde gereksiz tablo taramalarını azaltır.

## İncelenen sıcak sorgular

- Kullanıcının etkinlik listesi
- Etkinlik fotoğraf listesi
- Etkinlik export listesi
- Kullanıcı bildirim akışı
- Okunmamış bildirim sayısı
- Orphan storage dosyası referans kontrolleri
- Outbox işlenebilir mesaj taraması

## Değişiklikler

### Events

Sorgu:

```sql
WHERE OwnerUserId = @ownerUserId
ORDER BY EventDateUtc DESC, CreatedAtUtc DESC
```

Yeni index:

```text
IX_Events_OwnerUserId_EventDateUtc_CreatedAtUtc_Id
```

`Id`, aynı tarih değerlerinde deterministik tie-breaker ve gelecekte cursor pagination desteği için index sonuna eklenmiştir.

### EventGuestPhotos

Fotoğraf sorgusu artık şu sırayı kullanır:

```sql
ORDER BY CreatedAtUtc DESC, Id DESC
```

Yeni indexler:

```text
IX_EventGuestPhotos_EventId_CreatedAtUtc_Id
IX_EventGuestPhotos_StorageKey
```

`StorageKey` indexi orphan cleanup sırasında DB referans kontrolünü hızlandırır.

### GalleryExports

Yeni indexler:

```text
IX_GalleryExports_EventId_CreatedAtUtc_Id
IX_GalleryExports_StorageKey
```

`StorageKey` indexi nullable kayıtlar için partial index olarak tanımlanmıştır.

### Notifications

Eski index:

```text
OwnerUserId, DeletedAtUtc, ReadAtUtc, CreatedAtUtc
```

Genel bildirim listesinde `ReadAtUtc` filtrelenmediği için `CreatedAtUtc` sıralamasına doğrudan hizmet etmiyordu.

Yeni partial indexler:

```text
IX_Notifications_OwnerUserId_CreatedAtUtc_Id_Active
IX_Notifications_OwnerUserId_Unread
IX_Notifications_EventId_CreatedAtUtc_Id_Active
```

Soft-delete query filter ile aynı predicate kullanılmıştır:

```sql
WHERE DeletedAtUtc IS NULL
```

Okunmamış sayaç için daha küçük bir partial index kullanılır:

```sql
WHERE DeletedAtUtc IS NULL AND ReadAtUtc IS NULL
```

## Silinen indexler

Aşağıdaki indexler yeni indexlerin sol-prefix ve sıralama kapsamı nedeniyle kaldırılmıştır:

```text
IX_Events_OwnerUserId_EventDateUtc
IX_EventGuestPhotos_EventId_CreatedAtUtc
IX_GalleryExports_EventId_CreatedAtUtc
IX_Notifications_OwnerUserId_DeletedAtUtc_ReadAtUtc_CreatedAtUtc
IX_Notifications_EventId_DeletedAtUtc_CreatedAtUtc
```

## Migration

```text
20260718143000_OptimizePostgreSqlIndexes
```

## Production doğrulama sorguları

Migration sonrasında gerçek veri üzerinde aşağıdaki araçlar kullanılmalıdır:

```sql
EXPLAIN (ANALYZE, BUFFERS)
```

Index kullanım istatistikleri:

```sql
SELECT
    schemaname,
    relname,
    indexrelname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

Kullanılmayan indexler yalnızca yeterli trafik ve istatistik toplandıktan sonra değerlendirilmelidir.

## Önemli bulgu: model snapshot drift

Mevcut `OrtakareDbContextModelSnapshot`, aktif DbContext içindeki bazı sonradan eklenmiş entity'leri tam olarak içermemektedir. Bu durum bu index migration'ından önce de mevcuttur. Yeni migration manuel ve açık olarak yazılmıştır; ancak sonraki adımlarda snapshot'ın temiz bir PostgreSQL veritabanı üzerinden yeniden üretilmesi gerekir.

Snapshot düzeltilmeden otomatik `dotnet ef migrations add` çıktısı güvenilir kabul edilmemelidir.

## Doğrulama durumu

GitHub Actions ödeme engeli nedeniyle migration, build ve testler CI üzerinde doğrulanmamıştır. Production öncesinde temiz PostgreSQL üzerinde migration zinciri ve `EXPLAIN ANALYZE` çıktıları kontrol edilmelidir.
