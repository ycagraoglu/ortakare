# 012 — Accessibility

## Amaç

Ortakare frontend uygulamasında klavye, ekran okuyucu, focus yönetimi ve hareket azaltma ihtiyaçlarını ortak bir standartla ele almak.

## Route erişilebilirliği

`RouteAccessibility` route handle metadata içindeki başlığı kullanır.

Her route değişiminde:

- `document.title` güncellenir,
- ekran okuyucuya yeni sayfa adı `aria-live` bölgesiyle duyurulur,
- ilk uygulama yüklemesi hariç focus `#main-content` alanına taşınır,
- ana içerik görünür bölgeye kaydırılır.

`main` elemanları programatik focus için `tabIndex={-1}` kullanır.

## Skip link

Owner ve public layout başlangıcında `Ana içeriğe geç` bağlantısı bulunur. Bağlantı normal durumda görsel olarak gizlidir; klavye focus'u aldığında görünür hale gelir.

## Focus görünürlüğü

Link, button, input, select, textarea ve tabindex alanlarında `:focus-visible` outline kaldırılmaz. Mouse kullanıcısını gereksiz biçimde etkilemeden klavye odağı görünür tutulur.

## Reduced motion

`prefers-reduced-motion: reduce` tercihinde animasyon ve geçiş süreleri minimuma indirilir; smooth scrolling kapatılır.

## Form standardı

Mevcut `FormField` aşağıdaki ilişkileri korur:

- label ile input arasında `htmlFor` / `id`,
- hata halinde `aria-invalid`,
- hint ve hata metni için `aria-describedby`,
- hata duyurusu için `role=alert`.

Form submit hataları da `FormError` üzerinden `role=alert` ile duyurulur.

## Dialog standardı

`AccessibleDialog` native `<dialog>` ve `showModal()` kullanır.

Sağlanan davranışlar:

- modal semantiği ve tarayıcı focus sınırı,
- Escape ile kapatma,
- başlık için `aria-labelledby`,
- açıklama için `aria-describedby`,
- kapatma sonrasında tetikleyici elemana focus dönüşü,
- görünür ve erişilebilir kapatma butonu.

Yeni modal implementasyonları dağınık div tabanlı overlay yerine bu primitive'i kullanmalıdır.

## Kontrol listesi

- Tüm interaktif öğeler yalnızca klavyeyle kullanılabilmeli.
- Focus sırası DOM sırasını izlemeli.
- Pozitif tabindex kullanılmamalı.
- Sadece ikon içeren butonlarda erişilebilir ad bulunmalı.
- Renk tek başına durum iletmemeli.
- Modal kapanınca focus tetikleyiciye dönmeli.
- Route değişiminde sayfa başlığı ve ana içerik focus'u doğrulanmalı.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test, build, axe/Lighthouse denetimi, NVDA/VoiceOver testi ve yalnızca klavye smoke testi henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
