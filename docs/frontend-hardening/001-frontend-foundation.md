# 001 — Frontend Foundation

## Amaç

Ortakare frontend uygulaması için production hardening çalışmalarının üzerine kurulacağı güvenli ve katı tabanı oluşturmak.

## Eklenen temel yapı

- React 19
- TypeScript strict mode
- Vite
- TanStack Query
- React Router
- Zod environment validation
- Vitest ve Testing Library bağımlılıkları
- ESLint type-aware kuralları
- `@/` path alias

## Script standardı

```bash
npm run dev
npm run typecheck
npm run lint
npm run test
npm run build
npm run preview
```

## TypeScript kararları

Aşağıdaki kurallar özellikle açıktır:

- `strict`
- `noUncheckedIndexedAccess`
- `exactOptionalPropertyTypes`
- `noUnusedLocals`
- `noUnusedParameters`
- `noFallthroughCasesInSwitch`

Amaç, runtime'a taşınabilecek belirsizlikleri mümkün olduğunca derleme aşamasında yakalamaktır.

## Environment validation

`VITE_API_URL`, uygulama başlamadan önce Zod ile doğrulanır. Eksik veya geçersiz configuration ile uygulamanın sessizce yanlış API'ye bağlanmasına izin verilmez.

## TanStack Query başlangıç politikası

- Query stale time: 30 saniye
- Garbage collection: 5 dakika
- Query retry: 1
- Mutation retry: 0
- Window focus refetch: kapalı

Bu değerler global başlangıç varsayımlarıdır. Feature bazlı query'ler gerektiğinde açıkça override edilecektir.

## Klasör yönü

```text
src/
├── app/
├── features/
├── shared/
│   ├── api/
│   ├── config/
│   ├── lib/
│   └── ui/
└── routes/
```

Feature kodu `features` altında dikey olarak tutulacaktır. Genel altyapı ve tekrar kullanılabilir primitive'ler `shared` altında yer alacaktır.

## Doğrulama durumu

Dosyalar GitHub branch'ine yazılmıştır. Bu ortamda `npm install`, lint, typecheck, test veya build çalıştırılmamıştır. CI kanıtı oluşmadan foundation tamamlanmış kabul edilmeyecektir.
