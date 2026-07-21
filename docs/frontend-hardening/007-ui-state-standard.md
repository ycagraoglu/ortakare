# 007 — UI State Standardı

## Amaç

Ortakare frontend uygulamasındaki tüm feature ekranlarında loading, skeleton, empty, error, retry ve disabled durumlarını aynı erişilebilirlik ve görsel davranış kurallarıyla yönetmek.

## Kurulan yapı

```text
frontend/src/shared/ui/state/
├── AsyncState.tsx
├── LoadingState.tsx
├── Skeleton.tsx
├── StatePanel.tsx
├── index.ts
└── ui-state.css
```

## Standart durum sırası

Bir server-state ekranı aşağıdaki sırayı izler:

```text
Loading
→ Error
→ Empty
→ Success
```

Mutation işlemlerinde submit/action butonu işlem sürerken disabled olmalıdır.

## Loading ve skeleton

`LoadingState`, ekran okuyucular için `role="status"` ve görünmeyen açıklama taşır. Skeleton animasyonu `prefers-reduced-motion` açık olduğunda devre dışı kalır.

Route-level `Suspense` fallback artık aynı `LoadingState` bileşenini kullanır. Inline ve feature'a özel geçici skeleton yazılmamalıdır.

## Empty state

`EmptyState` şu bilgileri taşır:

- Kullanıcıya ne olmadığını açıklayan başlık
- Sonraki adımı anlatan açıklama
- Uygunsa birincil aksiyon

Boş liste normal bir durumdur; hata ekranı olarak sunulmaz.

## Error ve retry

`ErrorState`, erişilebilir hata duyurusu için `role="alert"` kullanır. Yeniden denenebilir sorgularda `Tekrar dene` aksiyonu verilmelidir.

`AsyncState` loading ve error dallarını standartlaştırır; başarılı içeriği children olarak render eder. Empty kontrolü feature'ın veri semantiğine ait olduğu için feature seviyesinde yapılır.

## Disabled-state

İşlem tamamlanana kadar kullanılamayan butonlar native `disabled` kullanmalıdır. Bilgilendirme amaçlı devre dışı öğelerde gerekirse `aria-disabled="true"` da eklenebilir.

Disabled öğe yalnızca görünüşte soluklaştırılmamalı; gerçekten etkileşim alamamalıdır.

## İlk uygulamalar

- Route yükleme ekranı ortak `LoadingState` kullanır.
- Events sayfası placeholder yerine ortak `EmptyState` kullanır.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
