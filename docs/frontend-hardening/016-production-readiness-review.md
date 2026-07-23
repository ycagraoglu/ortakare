# 016 — Production Readiness Review

## Amaç

Bu adımın amacı frontend uygulamasının yalnızca build alınabilir olmasını değil; tekrarlanabilir, doğrulanabilir, geri alınabilir ve IIS üzerinde güvenli şekilde yayınlanabilir olmasını sağlamaktır.

Bu doküman bir başarı beyanı değildir. Aşağıdaki kontroller gerçek ortamda çalıştırılmadan release `GO` kabul edilmez.

## Release kalite kapısı

Production adayı için önerilen sıra:

```bash
cd frontend
npm install
npm run verify:release
```

`verify:release` şu zinciri çalıştırır:

1. TypeScript typecheck
2. ESLint
3. Test ve coverage
4. Production build
5. Bundle budget
6. Release readiness kontrolleri

`package-lock.json` commit edilmeden release oluşturulmaz. Lockfile oluşturulduktan sonra temiz kurulum ayrıca doğrulanmalıdır:

```bash
rm -rf node_modules
npm ci
npm run verify:release
```

Windows PowerShell karşılığı:

```powershell
Remove-Item -Recurse -Force node_modules
npm ci
npm run verify:release
```

## Production environment

Build öncesinde en az aşağıdaki değerler açıkça tanımlanmalıdır:

```env
VITE_API_URL=https://api.example.com
VITE_RELEASE=<git-sha-or-build-number>
VITE_TELEMETRY_URL=
```

Kurallar:

- `VITE_API_URL` HTTPS olmalıdır.
- API origin'i backend CORS allowlist içinde bulunmalıdır.
- `VITE_RELEASE` gerçek commit SHA veya build numarası olmalıdır.
- Telemetry backend endpoint'i hazır değilse `VITE_TELEMETRY_URL` boş bırakılmalıdır.
- Secret, token ve parola hiçbir `VITE_*` değişkenine yazılmamalıdır. Vite değişkenleri browser bundle içine girer.

## Otomatik release kontrolleri

Yeni komut:

```bash
npm run check:release
```

Kontrol edilenler:

- `package-lock.json` varlığı
- `dist/index.html` varlığı
- `dist/web.config` varlığı
- IIS SPA fallback kuralı
- CSP, Referrer-Policy ve X-Content-Type-Options başlıkları
- `dist/sw.js` varlığı
- service worker cache version ve API cache dışı bırakma kuralı
- boş asset dosyası bulunmaması
- hash'li asset adlandırması için uyarı
- production API URL HTTPS kontrolü
- release sürümü uyarısı

Kritik hata varsa komut exit code `1` döndürür.

## IIS deployment modeli

Önerilen klasör modeli:

```text
C:\sites\ortakare-frontend\
├── releases\
│   ├── 20260723-140000-a1b2c3d\
│   └── 20260724-090000-d4e5f6g\
├── current\
└── backups\
```

Basit kopyala-değiştir yöntemi kullanılacaksa deployment öncesinde mevcut `current` klasörü tarih ve release bilgisiyle yedeklenmelidir.

Deployment sırası:

1. `npm ci`
2. Production environment değerlerini doğrula
3. `npm run verify:release`
4. `dist` çıktısını yeni release klasörüne kopyala
5. `web.config`, `sw.js`, manifest ve assets varlığını doğrula
6. IIS uygulama fiziksel yolunu yeni release klasörüne geçir veya atomik klasör değişimi yap
7. Application Pool recycle et
8. Smoke test çalıştır
9. Hata varsa önceki release'e rollback yap

Aynı klasör üzerinde dosyaları tek tek ezmek, kullanıcıların eski `index.html` ile yeni/eksik asset kombinasyonuna düşmesine neden olabilir. Yeni release ayrı klasöre hazırlanmalı ve geçiş mümkün olduğunca atomik yapılmalıdır.

## Cache invalidation

Vite üretim asset'leri içerik hash'i taşımalıdır ve uzun süre cache edilebilir. Buna karşılık aşağıdaki dosyalar eski sürümü işaret etmemelidir:

- `/index.html`
- `/sw.js`
- `/manifest.webmanifest`

Önerilen IIS politikası:

- Hash'li `/assets/*`: uzun süreli immutable cache
- `index.html`: `no-cache`
- `sw.js`: `no-cache`
- manifest: kısa cache veya `no-cache`

Service worker cache sürümü release davranışı değiştiğinde artırılmalıdır:

```js
const CACHE_VERSION = "ortakare-shell-v3";
```

Her release'te zorunlu artırmak yerine cache stratejisi veya app-shell içeriği değiştiğinde artırılması yeterlidir. Ancak deploy sonrası eski shell sorunu görülürse ilk kontrol noktası bu değerdir.

## Production smoke test

Deploy sonrası komut:

```bash
SMOKE_BASE_URL=https://app.example.com npm run smoke
```

PowerShell:

```powershell
$env:SMOKE_BASE_URL="https://app.example.com"
npm run smoke
```

Kontrol edilenler:

- `/` HTTP başarılı ve HTML dönüyor
- var olmayan SPA route'u HTML'e fallback oluyor
- manifest erişilebilir
- service worker erişilebilir
- CSP mevcut
- Referrer-Policy mevcut
- X-Content-Type-Options mevcut
- X-Frame-Options mevcut

Bu script authenticated iş akışlarını doğrulamaz. Manuel smoke kontrolü ayrıca yapılmalıdır.

## Manuel kritik kullanıcı akışları

Production veya staging üzerinde:

- Login
- Session restore
- Logout
- Yetkisiz route yönlendirmesi
- Etkinlik listeleme
- Etkinlik oluşturma/güncelleme
- Katılımcı ekleme
- Fotoğraf upload
- Upload iptali ve tekrar deneme
- Galeri görüntüleme
- Export/download
- 401 refresh akışı
- Offline ekranı
- Service worker update bildirimi
- Klavye ile temel navigasyon
- Mobil viewport kontrolü

Gerçek müşteri verisiyle test yapılmamalıdır. Ayrı test hesabı ve test etkinliği kullanılmalıdır.

## Backend ve CORS kontrolü

Release öncesinde:

- Frontend origin backend CORS allowlist içinde olmalı
- `OPTIONS` preflight başarılı olmalı
- `X-Correlation-Id` request ve response boyunca korunmalı
- 401 response refresh akışıyla uyumlu olmalı
- Upload limitleri frontend ve backend arasında aynı olmalı
- Rate limit davranışı kullanıcıya anlaşılır hata dönmeli
- API health endpoint'i izlenebilir olmalı

## Security kontrolü

- HTTPS zorunlu
- HTTP → HTTPS yönlendirmesi
- TLS sertifikası geçerli
- CSP gerçek production API ve telemetry origin'lerini kapsıyor
- Source map'lerin public yayını bilinçli karar olmalı
- Directory browsing kapalı
- Server version header mümkün olduğunca azaltılmış
- Token localStorage içinde bulunmuyor
- Query string içinde token yok
- `web.config` deployment çıktısında mevcut

Not: Mevcut Vite config production source map üretmektedir. Source map'ler public sunucuda tutulacaksa kaynak kod görünürlüğü kabul edilmiş olmalıdır. Aksi durumda production build için source map kapatılmalı veya ayrı, erişimi kısıtlı artifact olarak saklanmalıdır.

## Observability kontrolü

- `VITE_RELEASE` gerçek sürümü gösteriyor
- Frontend hata event'leri PII içermiyor
- Correlation ID kullanıcı hata ekranından backend loguna kadar izlenebiliyor
- Telemetry endpoint aktifse CORS ve rate limit uygulanmış
- Deploy başlangıç/bitiş zamanı kayıt altına alınmış
- İlk 30–60 dakika 4xx/5xx, frontend error ve Web Vitals izlenmiş

## Rollback planı

Rollback tetikleyicileri:

- Uygulama root veya SPA route açılmıyor
- Login yapılamıyor
- Kritik API çağrıları CORS nedeniyle engelleniyor
- Upload veya galeri temel akışı çalışmıyor
- JavaScript chunk yükleme hataları yaygın
- Hata oranı önceki release'e göre belirgin yükseliyor

Rollback sırası:

1. Yeni release'e trafik vermeyi durdur
2. IIS fiziksel yolunu önceki doğrulanmış release klasörüne döndür
3. Application Pool recycle et
4. `/`, SPA fallback ve login smoke testlerini tekrar çalıştır
5. Gerekirse service worker cache version ile düzeltme release'i hazırla
6. Başarısız release'i silmeden log ve artifact'leri koru

Database migration frontend rollback'ten bağımsız değerlendirilmelidir. Backend değişikliği backward-compatible değilse yalnız frontend rollback yeterli olmayabilir.

## GO / NO-GO kararı

### GO için zorunlu

- [ ] `package-lock.json` mevcut ve commitli
- [ ] Temiz `npm ci` başarılı
- [ ] `npm run verify:release` başarılı
- [ ] Production environment değerleri doğrulandı
- [ ] `dist` artifact arşivlendi
- [ ] IIS staging deploy başarılı
- [ ] Otomatik smoke test başarılı
- [ ] Kritik manuel akışlar başarılı
- [ ] CORS ve authentication doğrulandı
- [ ] Rollback klasörü hazır
- [ ] Release sahibi ve deploy zamanı belli

### NO-GO nedenleri

- Lockfile eksik
- Typecheck/lint/test/build başarısız
- Coverage veya bundle budget aşılmış
- Production API URL yanlış veya HTTP
- CSP API bağlantısını engelliyor
- SPA fallback çalışmıyor
- Login, upload veya galeri kritik akışı başarısız
- Rollback alınmamış

## Mevcut karar

Bu commit itibarıyla sonuç **NO-GO** durumundadır.

Nedenler:

- `package-lock.json` repository'de bulunmuyor.
- `npm install`, `npm ci` ve `npm run verify:release` çalıştırılmadı.
- Gerçek IIS deployment yapılmadı.
- Production smoke ve manuel kritik akış testleri yapılmadı.
- Production environment ve CORS doğrulanmadı.

Bu maddeler tamamlandıktan sonra kontrol listesi yeniden değerlendirilmelidir.
