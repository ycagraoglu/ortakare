# 014 — Frontend Test Strategy

## Amaç

Ortakare frontend uygulamasında testlerin yalnızca satır çalıştıran kontroller değil; kullanıcı davranışını, güvenlik sınırlarını ve backend sözleşmelerini koruyan üretim araçları olarak standartlaştırılması.

## Test piramidi

### Unit test

Saf fonksiyonlar ve küçük durum makineleri unit test ile doğrulanır.

Örnek alanlar:

- API result normalizasyonu,
- query retry politikası,
- query-key factory,
- upload validation,
- telemetry sanitization,
- auth storage politikası,
- URL güvenlik yardımcıları.

### Component test

Bileşenler DOM çıktısı ve kullanıcı davranışı üzerinden test edilir.

Örnek alanlar:

- form label ve hata ilişkileri,
- loading / empty / error state,
- erişilebilir dialog,
- upload seçme, iptal ve tekrar deneme,
- login form doğrulaması.

Implementation detail, internal state veya CSS class test edilmemelidir. Tercih sırası erişilebilir role, label ve kullanıcıya görünen metindir.

### Integration test

Birden fazla uygulama katmanının birlikte davranışı MSW ile doğrulanır.

Örnek alanlar:

- login request → session state → yönlendirme,
- 401 → tek refresh request → original request retry,
- validation response → form field errors,
- query success / error / retry davranışı,
- upload progress / cancellation / idempotency adapter davranışı.

### E2E test

E2E altyapısı bu adımda kurulmamıştır. Production readiness öncesinde ayrı tarayıcı otomasyonu ile aşağıdaki kritik akışlar ele alınmalıdır:

- kayıt ve giriş,
- etkinlik oluşturma,
- katılımcı ekleme,
- fotoğraf upload,
- galeri görüntüleme,
- export başlatma,
- logout ve session expiry.

## Araçlar

- Vitest: test runner,
- React Testing Library: component ve integration testleri,
- user-event: gerçekçi klavye ve pointer etkileşimleri,
- MSW: network sınırında API mock,
- jest-dom: erişilebilir DOM assertion'ları,
- V8 coverage: coverage raporu.

## Ortak test setup

`frontend/src/test/setup-tests.ts` her test için:

- jest-dom matcher'larını yükler,
- MSW server'ı başlatır,
- mock edilmemiş request'i hata kabul eder,
- test sonunda DOM cleanup yapar,
- MSW handler'larını sıfırlar,
- localStorage ve sessionStorage alanlarını temizler.

Mock edilmemiş request'in hata sayılması, testin yanlışlıkla gerçek API'ye çıkmasını veya eksik sözleşmeyle sessizce geçmesini engeller.

## Ortak render helper

`frontend/src/test/render.tsx`:

- temiz QueryClient,
- MemoryRouter,
- QueryClientProvider,
- user-event instance

sağlar.

Feature testleri kendi provider kopyalarını oluşturmamalı; gerekli ek provider'lar bu helper üzerinden genişletilmelidir.

## MSW standardı

API mock'ları axios veya fetch fonksiyonunu doğrudan mock etmek yerine HTTP sınırında kurulmalıdır.

Doğru yaklaşım:

```ts
server.use(
  http.get("https://api.example.com/api/events", () =>
    HttpResponse.json({ isSuccess: true, data: [] }),
  ),
);
```

Kaçınılması gereken yaklaşım:

```ts
vi.mock("axios");
```

HTTP sınırında mock kullanmak interceptor, header, error normalization ve response contract davranışlarını birlikte test eder.

## Test verisi politikası

- Gerçek kullanıcı verisi kullanılmaz.
- Token benzeri değerler sahte ve açıkça test amaçlı olur.
- Tarihler sabitlenir veya fake timer kullanılır.
- UUID gereken testlerde deterministic değer tercih edilir.
- Testler çalışma sırasına bağımlı olmaz.
- Network, storage ve timer state her testte temizlenir.

## Assertion politikası

Tercih sırası:

1. `getByRole`,
2. `getByLabelText`,
3. kullanıcıya görünen metin,
4. gerektiğinde test-id.

Aşağıdakiler doğrudan test hedefi yapılmamalıdır:

- private fonksiyonlar,
- hook iç state'i,
- component implementation detail,
- yalnızca className,
- snapshot ile büyük DOM çıktıları.

## Coverage politikası

Başlangıç eşikleri:

- statements: %70,
- lines: %70,
- functions: %70,
- branches: %65.

Coverage kalite göstergelerinden yalnızca biridir. Yüzdeyi yükseltmek için anlamsız assertion veya implementation-detail testi yazılmamalıdır.

Yeni güvenlik, auth, API, upload ve para/izin etkili kodlar kendi davranış testleri olmadan tamamlanmış sayılmamalıdır.

## Eklenen regresyon testleri

### Auth storage

Doğrulanan kurallar:

- refresh token yalnızca sessionStorage'a yazılır,
- kullanıcı bilgisi yalnızca sessionStorage'a yazılır,
- remembered email dışında localStorage'a auth verisi yazılmaz,
- refresh rotation kalıcı storage oluşturmaz,
- logout eski deployment'tan kalan legacy değerleri de temizler.

### Telemetry sanitization

Doğrulanan kurallar:

- query string ve hash kaldırılır,
- GUID ve sayısal route parametreleri `:id` olur,
- e-posta `[email]` olarak maskelenir,
- identifier değerleri `[id]` olarak maskelenir,
- absolute API URL yalnızca temizlenmiş path'e dönüştürülür.

### Form accessibility

Doğrulanan kurallar:

- label input ile ilişkilidir,
- hint ve error `aria-describedby` içinde bulunur,
- invalid input `aria-invalid=true` alır,
- field ve form hataları `role=alert` ile duyurulur.

## Scriptler

```bash
npm run test
npm run test:watch
npm run test:coverage
```

## CI beklentisi

015 Frontend CI adımında en az şu sıra zorunlu olacaktır:

```bash
npm ci
npm run typecheck
npm run lint
npm run test:coverage
npm run build
```

Herhangi biri başarısız olursa PR check başarısız olmalıdır.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. Yeni dependency'ler nedeniyle lockfile henüz güncellenmemiştir. `npm ci`, typecheck, lint, test, coverage ve build komutları çalıştırılmamıştır; başarılı kabul edilmemelidir.
