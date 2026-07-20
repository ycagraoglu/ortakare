# 005 — Routing ve Code Splitting

## Amaç

Ortakare frontend uygulamasında public ve authenticated owner yüzeylerini birbirinden ayıran, route metadata kullanan, gerçek feature-level lazy loading sağlayan ve merkezi navigation registry ile çalışan router omurgası kurmak.

## Kurulan yapı

```text
frontend/src/app/
├── layouts/
│   ├── OwnerLayout.tsx
│   ├── PublicLayout.tsx
│   └── owner-layout.css
├── navigation/
│   ├── OwnerSidebar.tsx
│   ├── OwnerTopbar.tsx
│   └── owner-navigation.ts
└── router/
    ├── lazy-route.ts
    ├── route-breadcrumbs.tsx
    ├── route-error.tsx
    ├── route-guards.tsx
    ├── route-loading.tsx
    ├── route-meta.ts
    ├── route-modules.ts
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
- Session `initializing` durumundayken route loading ekranı gösterilir.

## Metadata

Her route başlık, breadcrumb ve gelecekte permission bilgisi taşıyabilecek `RouteMeta` sözleşmesine sahiptir.

Metadata şu amaçlarla kullanılır:

- `document.title` üretimi
- breadcrumb üretimi
- gelecekte permission kontrolü
- route tabanlı navigation davranışı

## Gerçek feature-level lazy loading

Business sayfaları ortak bir route dosyasında tutulmaz. Her sayfa kendi feature klasöründen dynamic import edilir:

```text
features/dashboard/pages/DashboardPage.tsx
features/events/pages/EventsPage.tsx
features/photos/pages/PhotosPage.tsx
```

`route-modules.ts`, lazy componentleri ve preload fonksiyonlarını merkezi olarak tanımlar. Böylece router ile navigation aynı module registry'yi kullanır ve circular dependency oluşmaz.

## Navigation registry

Sidebar öğeleri `owner-navigation.ts` içinde tek kaynaktan yönetilir. Her öğe:

- route key
- URL
- kullanıcı etiketi
- kısa açıklama
- preload fonksiyonu
- exact match davranışı

taşır.

Yeni owner modülü eklenirken route, lazy module ve navigation kaydı birlikte açıkça tanımlanır.

## Prefetch davranışı

Sidebar bağlantısına mouse ile gelindiğinde veya klavye ile focus verildiğinde ilgili feature chunk arka planda yüklenir.

Preload hatası navigation'ı bozmaz; asıl route yüklemesi hata boundary tarafından yönetilir.

## Owner shell

`OwnerLayout` yalnızca uygulama kabuğunu birleştirir:

```text
OwnerSidebar
OwnerTopbar
RouteBreadcrumbs
Outlet
```

Sidebar ve topbar ayrı componentlerdir. Layout masaüstünde iki kolonlu, dar ekranlarda tek kolonlu responsive düzene geçer.

## Breadcrumb

Breadcrumb route metadata üzerinden otomatik üretilir. Son öğe `aria-current="page"` ile işaretlenir; önceki öğeler navigasyon bağlantısıdır.

## Hata yönetimi

Route error boundary aşağıdaki senaryoları kapsar:

- route response hataları
- beklenmeyen render/yükleme hataları
- eski deploy chunk'ı nedeniyle dynamic import hatası

Chunk yükleme hatasında yalnızca bir kez otomatik yenileme denenir. Sonsuz reload döngüsü `sessionStorage` anahtarıyla engellenir.

## Bilinçli olarak sonraya bırakılanlar

- Gerçek login/register formları
- Gerçek feature ekranları
- Role/permission verisinin auth session'a eklenmesi
- Feature özel skeleton componentleri
- Mobil drawer sidebar davranışı
- Offline durumunun browser eventleriyle otomatik yönlendirilmesi
- Route transition animasyonları

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
