# 003 — Authentication Session

## Amaç

Ortakare owner oturumunu merkezi, test edilebilir ve güvenli bir frontend katmanında yönetmek.

## Backend sözleşmesi

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

Login response access token, refresh token, süre bilgileri ve kullanıcı özetini döndürür. Refresh endpoint refresh token rotation uygular.

## Uygulanan kararlar

- Access token yalnızca memory içinde tutulur.
- Access token `localStorage`, `sessionStorage`, URL veya loglara yazılmaz.
- Refresh token, kullanıcının kalıcı oturum seçimine göre `localStorage` veya `sessionStorage` içinde tutulur.
- Login ve refresh endpointleri otomatik 401 refresh davranışının dışında bırakılır.
- Birden fazla request aynı anda 401 döndürürse tek refresh promise paylaşılır.
- Refresh başarılı olursa bekleyen request yeni access token ile yalnızca bir kez tekrar edilir.
- Refresh başarısız olursa session temizlenir ve kullanıcı anonymous duruma geçirilir.
- Logout backend çağrısı başarısız olsa bile yerel session temizlenir.
- Uygulama açılışında refresh token varsa session restore denenir.

## Durum modeli

- `initializing`
- `authenticated`
- `anonymous`

React tarafı session durumunu `useSyncExternalStore` kullanan `useAuth` hook'u ile izler.

## Dosyalar

- `features/auth/api/auth-api.ts`
- `features/auth/model/auth-session.ts`
- `features/auth/model/auth-storage.ts`
- `features/auth/model/auth-types.ts`
- `features/auth/hooks/use-auth.ts`
- `features/auth/components/AuthBootstrap.tsx`
- `shared/api/auth-bridge.ts`
- `shared/api/axios.ts`

## Güvenlik sınırı

Refresh token'ın browser storage içinde tutulması XSS riskine karşı tamamen bağışık değildir. Backend mevcut sözleşmede refresh token'ı JSON response/body üzerinden yönettiği için bu V1 uyarlamasıdır. Gelecekte backend HttpOnly, Secure, SameSite cookie modeline geçirilirse storage katmanı kaldırılmalıdır.

## Doğrulama

Aşağıdaki komutlar çalıştırılmadan madde doğrulanmış sayılmaz:

```bash
npm ci
npm run typecheck
npm run lint
npm run test
npm run build
```
