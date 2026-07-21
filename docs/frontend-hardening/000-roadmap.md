# Frontend Production Hardening Roadmap

## Amaç

Ortakare frontend uygulamasını React, TypeScript ve Vite tabanlı; güvenli, gözlemlenebilir, test edilebilir ve bağımsız deploy edilebilir bir PWA olarak hazırlamak.

## Başlangıç durumu

Frontend klasöründe yalnızca planlama README'si bulunmaktadır. Bu nedenle seri, mevcut uygulamayı iyileştirmekten önce production-ready foundation kurarak başlayacaktır.

## Yol haritası

1. ✅ **Frontend Foundation** — Vite, React, TypeScript strict mode, klasör yapısı, environment doğrulaması, temel scriptler.
2. ✅ **API Client Standardı** — merkezi HTTP client, ApiResult normalizasyonu, timeout, cancellation, hata sınıflandırması.
3. ✅ **Authentication Session** — access/refresh token akışı, tekilleştirilmiş refresh, session restore ve logout politikası.
4. ✅ **TanStack Query Standardı** — QueryClient, query-key factory, stale/cache süreleri, invalidation kuralları.
5. ✅ **Routing ve Code Splitting** — route grupları, lazy loading, owner/public yüzey ayrımı, metadata, breadcrumb, 404/403 ve chunk recovery.
6. ✅ **Form Standardı** — React Hook Form, Zod, backend validation eşlemesi, dirty-state koruması ve login referans uygulaması.
7. ✅ **UI State Standardı** — loading, skeleton, empty, error, retry ve disabled-state kuralları.
8. ⏳ **Global Error Handling** — ErrorBoundary, chunk-load recovery, offline/network/timeout deneyimi.
9. ⏳ **Upload Hardening** — istemci ön kontrolü, progress, cancellation, idempotency header, güvenli preview.
10. ⏳ **PWA Hardening** — manifest, service worker stratejisi, update akışı, offline sınırları, cache güvenliği.
11. ⏳ **Frontend Security** — token saklama kararı, XSS/CSP uyumu, external URL ve download güvenliği.
12. ⏳ **Accessibility** — klavye, focus yönetimi, aria, form hata duyuruları ve modal davranışı.
13. ⏳ **Observability** — web vitals, istemci hata korelasyonu, request-id/trace-id görünürlüğü.
14. ⏳ **Test Strategy** — unit, component, integration ve kritik kullanıcı akışı testleri.
15. ⏳ **Frontend CI** — clean install, typecheck, lint, test, build ve bundle budget.
16. ⏳ **Production Readiness Review** — deployment, rollback, smoke test, cache invalidation ve GO/NO-GO.

## Bağlayıcı kurallar

- TypeScript `strict` açık olacaktır.
- Component içinde doğrudan `fetch` veya dağınık HTTP client kullanılmayacaktır.
- Server state TanStack Query ile; local UI state React state ile yönetilecektir.
- Access token URL, query string veya loglara yazılmayacaktır.
- Public guest yüzeyi ile authenticated owner yüzeyi route ve layout seviyesinde ayrılacaktır.
- Her tamamlanan madde `docs/frontend-hardening` altında numaralı dokümanla kayıt altına alınacaktır.
- Build/test sonucu çalıştırılmadan başarılı sayılmayacaktır.
