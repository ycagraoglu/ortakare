# 005 — Routing ve Code Splitting

## Amaç

Ortakare frontend uygulamasında public ve authenticated owner yüzeylerini birbirinden ayıran, route metadata kullanan, lazy yüklemeye ve route-level hata izolasyonuna hazır bir router omurgası kurmak.

## Kurulan yapı

```text
frontend/src/app/
├── layouts/
│   ├── OwnerLayout.tsx
│   └── PublicLayout.tsx
└── router/
    ├── route-breadcrumbs.tsx
    ├── route-error.tsx
    ├── route-guards.tsx
    ├── route-loading.tsx
    ├── route-meta.ts
    ├── route-pages.tsx
    └── router.tsx
```

## Route grupları

Public yüzey:

```text
/login
/register
/forbidden
/offline
```

Owner yüzeyi:

```text
/dashboard
/events
/participants
/photos
/gallery
/notifications
/settings
```

Bilinmeyen URL'ler `404` sayfasına gider.

## Guard standardı

- `ProtectedRoute`: authenticated olmayan kullanıcıyı `/login` sayfasına yönlendirir.
- `AnonymousRoute`: authenticated kullanıcıyı `/dashboard` sayfasına yönlendirir.
- `PermissionRoute`: gelecekteki rol/izin modelinin route ağacına bağlanması için hazırdır.
- Session `initializing` durumundayken route skeleton gösterilir.

## Metadata

Her route başlık, breadcrumb ve gelecekte permission bilgisi taşıyabilecek `RouteMeta` sözleşmesine sahiptir.

Metadata şu amaçlarla kullanılır:

- `document.title` üretimi
- breadcrumb üretimi
- gelecekte permission kontrolü
- gelecekte navigation registry üretimi

## Lazy loading

Sayfa componentleri `React.lazy` ve `Suspense` üzerinden yüklenir. İlk placeholder ekranlar ortak bir route page modülündedir. Gerçek feature modülleri yazıldığında her feature kendi `pages` giriş dosyasına taşınmalı ve ayrı dynamic import ile gerçek feature-level chunk ayrımı sağlanmalıdır.

## Hata yönetimi

Route error boundary aşağıdaki senaryoları kapsar:

- route response hataları
- beklenmeyen render/yükleme hataları
- eski deploy chunk'ı nedeniyle dynamic import hatası

Chunk yükleme hatasında yalnızca bir kez otomatik yenileme denenir. Sonsuz reload döngüsü `sessionStorage` anahtarıyla engellenir.

## Layout ayrımı

`PublicLayout` ve `OwnerLayout` nested route seviyesinde ayrılmıştır. Owner layout sidebar, header, kullanıcı bilgisi, logout ve breadcrumb alanlarını taşır.

## Bilinçli olarak sonraya bırakılanlar

- Gerçek login/register formları
- Gerçek feature ekranları
- Role/permission verisinin auth session'a eklenmesi
- Sidebar hover ile bundle prefetch
- Feature özel skeleton componentleri
- Offline durumunun browser eventleriyle otomatik yönlendirilmesi
- Route transition animasyonları

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
