# 010 — PWA Hardening

## Amaç

Ortakare frontend uygulamasına kurulabilir PWA kabiliyeti eklemek; service worker cache sınırlarını güvenli biçimde belirlemek; offline ve yeni sürüm deneyimini kullanıcıya açıkça göstermek.

## Manifest

`frontend/public/manifest.webmanifest` aşağıdaki bilgileri tanımlar:

- uygulama adı ve kısa adı,
- standalone görünüm,
- tema ve arka plan renkleri,
- uygulama kapsamı ve başlangıç URL'si,
- maskable kullanılabilen SVG ikon.

## Tek service worker kararı

Projede yalnızca `frontend/public/sw.js` kullanılır. Manuel worker ile build plugin tarafından üretilen ikinci bir worker birlikte çalıştırılmaz. Böylece kayıt, update ve cache sahipliği tek noktada kalır.

## Service worker güvenlik politikası

Service worker yalnızca aynı origin üzerindeki statik uygulama kabuğunu cache'ler.

Kesinlikle cache dışı bırakılan alanlar:

- `/api/*`,
- auth istekleri,
- upload ve download istekleri,
- gallery export içerikleri,
- farklı origin kaynakları,
- GET dışındaki tüm istekler.

Bu karar kullanıcıya özel API cevaplarının, token ilişkili içeriklerin veya büyük dosyaların browser cache'ine girmesini engeller.

## Cache stratejisi

- Navigasyon istekleri: network-first ve `cache: no-store`; başarısızsa cache'lenmiş uygulama kabuğu.
- Script, style, font ve statik image: cache-first, arka planda network güncellemesi.
- API ve mutation istekleri: service worker müdahalesi yok.
- Eski cache sürümleri activation sırasında temizlenir.

## Yeni sürüm akışı

Yeni service worker kurulup waiting durumuna geçtiğinde kullanıcıya yeni sürüm bildirimi gösterilir. Kullanıcı `Şimdi güncelle` dediğinde `SKIP_WAITING` mesajı gönderilir. Yeni worker kontrolü alınca sayfa bir kez yenilenir.

Güncelleme kullanıcı onayı olmadan çalışma ortasında zorla uygulanmaz. Uygulama açıkken worker güncellemesi saatte bir kontrol edilir.

`controllerchange`, online ve offline listener'ları component unmount sırasında temizlenir. Bu sayede tekrar mount senaryolarında biriken event listener oluşmaz.

## Offline sınırı

Offline banner açıkça şunu belirtir:

- önceden yüklenmiş ekranlar açılabilir,
- gönderme, güncelleme, upload ve diğer server işlemleri bağlantı gerektirir.

Ortakare offline-first veri düzenleme uygulaması değildir. Mutation queue, kullanıcı verisini IndexedDB'de saklama veya background sync bu aşamada bilinçli olarak uygulanmamıştır.

## Kurulum davranışı

Tarayıcının doğal install UI'sı kullanılır. Özel `beforeinstallprompt` butonu bu aşamada eklenmemiştir; platformlar arası farklı davranışlar nedeniyle install çağrısı ayrı UX çalışması olarak bırakılmıştır.

## Tarayıcı doğrulama kontrolü

1. Application > Manifest bölümünde manifest hatası bulunmamalıdır.
2. Service Workers bölümünde yalnızca tek worker görünmelidir.
3. Cache Storage içinde API, auth, upload, download veya export yanıtı bulunmamalıdır.
4. Offline modda daha önce açılmış uygulama kabuğu yüklenmelidir.
5. Yeni worker waiting durumuna geçtiğinde update bildirimi görünmelidir.
6. `Şimdi güncelle` sonrasında yalnızca bir reload gerçekleşmelidir.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test, production build, Lighthouse PWA denetimi ve gerçek cihaz install testi henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
