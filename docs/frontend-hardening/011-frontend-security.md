# 011 — Frontend Security

## Amaç

Ortakare frontend uygulamasında token saklama, güvenilir origin kontrolü, harici bağlantı açma, download yönlendirmesi ve IIS güvenlik başlıkları için bağlayıcı kurallar tanımlamak.

## Token saklama kararı

- Access token yalnızca JavaScript belleğinde tutulur.
- Refresh token yalnızca `sessionStorage` içinde tutulur.
- Refresh token ve kullanıcı oturum verisi `localStorage` içine yazılmaz.
- Tarayıcı veya sekme oturumu kapandığında refresh token kalıcı olarak korunmaz.
- `localStorage` yalnızca kullanıcının açıkça seçtiği hatırlanan e-posta adresini tutabilir.

Bu yaklaşım XSS riskini tamamen ortadan kaldırmaz; ancak uzun ömürlü refresh token'ın kalıcı browser storage içinde bulunmasını engeller.

Uzun süreli oturum isteniyorsa doğru nihai model refresh token'ın backend tarafından `HttpOnly`, `Secure` ve uygun `SameSite` ayarlarıyla cookie içinde yönetilmesidir.

## API origin koruması

Merkezi Axios client yalnızca `VITE_API_URL` ile aynı origin'e istek gönderebilir. Absolute URL veya değiştirilmiş `baseURL` kullanılarak farklı bir origin'e istek gönderilmeye çalışılırsa request, Authorization header eklenmeden reddedilir.

Production ortamında API URL'sinin HTTPS kullanması zorunludur.

## Harici URL standardı

`resolveTrustedUrl`, `openTrustedExternalUrl` ve `navigateToTrustedDownload` yardımcıları aşağıdaki kuralları uygular:

- `javascript:`, `data:`, `file:` ve benzeri protokoller reddedilir.
- Harici URL yalnızca HTTPS olabilir.
- Harici origin açık allowlist içinde bulunmalıdır.
- Yeni sekme `noopener,noreferrer` ile açılır.
- Download URL'si aynı origin veya açıkça izin verilen storage/CDN origin'i olmak zorundadır.

Component içinde doğrudan `window.open(apiValue)` veya kontrolsüz `window.location.assign(apiValue)` kullanılmamalıdır.

## IIS güvenlik başlıkları

`frontend/public/web.config` Vite build sırasında `dist/web.config` olarak kopyalanır. Dosya aşağıdaki başlıkları tanımlar:

- Content-Security-Policy
- Referrer-Policy
- X-Content-Type-Options
- X-Frame-Options
- Permissions-Policy
- Cross-Origin-Opener-Policy

CSP'de `object-src 'none'`, `frame-ancestors 'none'`, `base-uri 'self'` ve `form-action 'self'` kullanılır.

`style-src 'unsafe-inline'` mevcut inline React style kullanımları nedeniyle geçici olarak korunmuştur. Accessibility ve UI refactor sonrasında nonce/hash veya tamamen class tabanlı stil yaklaşımına geçilmesi hedeflenmelidir.

`connect-src` HTTPS bağlantılarına izin verse de token sızıntısı ayrıca Axios origin kontrolüyle engellenir. Production deployment sırasında mümkünse API ve telemetry origin'leri açık listeyle daraltılmalıdır.

## Referrer ve otomatik algılama

HTML dokümanı `no-referrer` politikası kullanır. Tarayıcının e-posta, telefon ve adres metinlerini otomatik linke çevirmesi kapatılmıştır.

## Kaçınılması gereken kullanımlar

- Token'ı URL, query string veya log içine yazmak.
- API'den gelen HTML'i `dangerouslySetInnerHTML` ile doğrudan render etmek.
- API'den gelen URL'yi allowlist kontrolü olmadan açmak.
- Refresh token'ı localStorage içine geri koymak.
- Harici origin'e merkezi `apiClient` ile istek göndermek.
- CSP'yi feature geliştirmek için tamamen kapatmak.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test, production build, IIS header doğrulaması, CSP violation testi ve browser security smoke testi henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
