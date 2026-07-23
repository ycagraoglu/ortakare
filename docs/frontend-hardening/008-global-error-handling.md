# 008 — Global Error Handling

## Amaç

Ortakare frontend uygulamasında router, provider, auth bootstrap ve component render hatalarını kontrollü bir kullanıcı deneyimiyle ele almak; network, offline, timeout, server ve chunk-load senaryolarını birbirinden ayırmak.

## Kurulan yapı

```text
frontend/src/shared/error/
├── ApplicationErrorBoundary.tsx
├── GlobalErrorFallback.tsx
├── error-utils.ts
├── error.css
└── index.ts
```

## Error boundary katmanları

- `ApplicationErrorBoundary`, router dışındaki ve uygulama kökündeki render hatalarını yakalar.
- `RouteErrorPage`, React Router route render ve lazy import hatalarını yakalar.
- Her iki katman aynı `GlobalErrorFallback` bileşenini kullanır.

## Hata sınıflandırması

Desteklenen hata türleri:

```text
chunk
offline
network
timeout
server
unknown
```

`ApiError` içindeki correlation-id global fallback ekranında destek kodu olarak gösterilir.

## Chunk-load recovery

Yeni deploy sonrasında eski HTML'in artık bulunmayan bir JavaScript chunk'ını istemesi halinde yalnızca bir otomatik yenileme yapılır.

```text
Chunk load error
→ sessionStorage kontrolü
→ ilk hata ise reload
→ tekrar hata olursa fallback ekranı
```

Bu yaklaşım sonsuz yenileme döngüsünü engeller.

## Offline ve network ayrımı

`navigator.onLine === false` durumunda kullanıcıya internet bağlantısı olmadığı açıklanır. Bağlantı mevcut görünmesine rağmen API'ye ulaşılamıyorsa hata `network` olarak sınıflandırılır.

## Güvenlik ve gözlemlenebilirlik

Production ekranında stack trace veya ham backend hata gövdesi gösterilmez. Correlation-id varsa destek kodu olarak sunulur. Development ortamında yakalanan uygulama hatası console'a yazılır.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
