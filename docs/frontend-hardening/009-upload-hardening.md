# 009 — Upload Hardening

## Amaç

Fotoğraf yükleme akışını istemci tarafında dosya ön kontrolü, güvenli preview, progress, cancellation ve idempotency kurallarıyla standartlaştırmak.

## Kurulan yapı

```text
frontend/src/shared/upload/
├── ImageUploadPanel.tsx
├── image-upload-policy.ts
├── upload-file.ts
├── upload-types.ts
├── use-file-preview.ts
├── use-upload-task.ts
├── validate-image-file.ts
├── upload.css
└── index.ts
```

## Güvenlik sınırı

İstemci doğrulaması yalnızca kullanıcı deneyimini iyileştirir. Güvenlik otoritesi backend'dir. Backend dosyanın gerçek imzasını, içerik türünü, boyutunu, dosya adını ve depolama politikasını yeniden doğrulamalıdır.

## Dosya ön kontrolü

Varsayılan politika:

- JPEG, PNG ve WebP
- En fazla 15 MB
- En fazla 180 karakter dosya adı
- Boş dosya reddi
- Kontrol karakteri içeren dosya adı reddi

Bu değerler backend `PhotoUploadOptions` ayarlarıyla aynı kaynaktan beslenene kadar iki tarafta ayrıca kontrol edilmelidir.

## Idempotency

Her seçilen dosya için `crypto.randomUUID()` ile bir `clientUploadId` oluşturulur. Aynı yükleme retry edildiğinde bu değer korunur. Yeni dosya seçildiğinde veya form sıfırlandığında yeni değer üretilir.

Multipart request üzerinde `X-Client-Upload-Id` header'ı gönderilir. Endpoint adapter backend'in gerçek header veya route sözleşmesine göre bağlanmalıdır.

## Progress ve cancellation

Axios `onUploadProgress` üzerinden byte ve yüzde bilgisi üretilir. Aktif request `AbortController` ile iptal edilir. İptal edilen request hata olarak değil `cancelled` state olarak ele alınır.

## Duplicate-submit koruması

`uploading` durumunda:

- yükle butonu disabled olur,
- dosya seçici disabled olur,
- ikinci upload operation başlatılmaz.

## Güvenli preview

Preview için `URL.createObjectURL` kullanılır. Dosya değiştiğinde veya component unmount olduğunda `URL.revokeObjectURL` çağrılır. Base64'e çevirme ve gereksiz memory büyümesi yapılmaz.

## Yeniden kullanılabilir panel

`ImageUploadPanel` endpoint bilmez. Feature katmanı aşağıdaki adapter'ı sağlar:

```tsx
<ImageUploadPanel
  upload={(file, options) =>
    uploadFile({
      url: endpoint,
      file,
      signal: options.signal,
      clientUploadId: options.clientUploadId,
      onProgress: options.onProgress,
      headers: participantHeaders,
    })
  }
/>
```

Bu sayede owner upload, guest gallery upload veya ileride toplu upload aynı UI standardını kullanabilir.

## Bilinçli olarak sonraya bırakılanlar

- Guest gallery route ve token adapter'ı
- Çoklu dosya queue yönetimi
- Paralel upload concurrency limiti
- Retry/backoff politikası
- EXIF yönlendirme ve client-side resize
- Upload sonrası query invalidation

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
