# 006 — Form Standardı

## Amaç

Ortakare frontend uygulamasındaki formların aynı doğrulama, hata gösterimi, submit ve kaydedilmemiş değişiklik davranışlarını kullanmasını sağlamak.

## Teknoloji kararı

- React Hook Form
- Zod 4
- `@hookform/resolvers/zod`
- Backend alan hataları için merkezi mapping
- SPA navigation ve browser refresh için dirty-state koruması

## Kurulan yapı

```text
frontend/src/shared/form/
├── apply-api-field-errors.ts
├── form-field.tsx
├── form.css
├── index.ts
└── use-unsaved-changes.ts
```

## Form sözleşmesi

Her feature formu aşağıdaki akışı izler:

```text
Zod Schema
→ React Hook Form
→ Shared Form Field
→ Feature submit handler
→ API
→ Backend field error mapping
```

Component içinde manuel doğrulama zincirleri veya dağınık hata eşlemesi kullanılmaz.

## Backend validation mapping

`ApiError`, backend response içindeki `errors` veya `validationErrors` sözlüğünü `fieldErrors` olarak korur.

`applyApiFieldErrors` PascalCase backend alan adlarını camelCase frontend alan adlarına dönüştürerek ilgili input'a bağlar.

Örnek:

```text
Email → email
Password → password
```

Alan eşlemesi yapılamayan veya genel hatalar form üstündeki `FormError` alanında gösterilir.

## Dirty-state koruması

`useUnsavedChanges` iki senaryoyu kapsar:

1. React Router içindeki SPA navigation.
2. Browser refresh, sekme kapatma veya sayfadan ayrılma.

Bu hook yalnızca gerçekten değişiklik yapılmış formlarda `formState.isDirty` ile etkinleştirilmelidir.

## Login referans uygulaması

Login ekranı placeholder olmaktan çıkarılmış ve form standardının ilk gerçek uygulaması yapılmıştır.

Özellikler:

- Zod email ve password doğrulaması
- `onBlur` validation
- Submit sırasında disabled button
- Backend field validation mapping
- Genel API hata alanı
- Remember-me seçimi
- Korunan route'tan gelinen sayfaya geri yönlendirme

## Bağlayıcı kurallar

- Feature form schema'sı feature klasörü içinde tutulur.
- API çağrısı shared form component içinde yapılmaz.
- Form field primitive'leri backend DTO bilmez.
- Submit sırasında çift tıklama engellenir.
- Backend hata mesajı yalnızca güvenli, kullanıcıya gösterilebilir sözleşmeden gelir.
- Dirty guard create/update formlarında kullanılır; login gibi kayıp veri riski taşımayan formlarda zorunlu değildir.

## Doğrulama durumu

Dosyalar repository'ye yazılmıştır. `npm ci`, typecheck, lint, test ve production build henüz çalıştırılmamıştır. Çalıştırılmadan başarılı kabul edilmemelidir.
