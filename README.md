# Ortakare

**Herkesin gözünden, tek bir hikâye.**

Ortakare; etkinlik sahiplerinin ortak bir dijital albüm oluşturmasını, misafirlerin QR kod ile katılarak ad veya rumuz seçmesini ve fotoğraflarını doğrudan yüklemesini sağlayan bağımsız bir etkinlik fotoğraf platformudur.

## Ürün Yapısı

```text
Etkinlik sahibi
  → kayıt olur
  → etkinlik oluşturur
  → QR kod üretir
  → katılımcıları ve fotoğrafları yönetir
  → toplu ZIP dışa aktarımı ister

Misafir
  → QR kodu okutur
  → adını veya rumuzunu girer
  → fotoğraf çeker veya galeriden seçer
  → fotoğrafı ortak albüme yükler
```

## Teknik Yığın

### Backend

- ASP.NET Core 10 LTS Web API
- Controller tabanlı Vertical Slice Architecture
- MediatR kullanılmaz
- Minimal API kullanılmaz
- EF Core 10
- PostgreSQL
- Hangfire
- Cloudflare R2

### Frontend

- React
- TypeScript
- Vite
- PWA
- TanStack Query
- shadcn/ui

## Repo Yapısı

```text
ortakare
├── backend
├── frontend
├── docs
│   └── architecture
└── README.md
```

Backend ve frontend aynı repoda bulunur ancak bağımsız şekilde build ve deploy edilebilir.

## Mimari Kurallar

Backend geliştirmelerinde bağlayıcı mimari kararlar için:

- [`ADR-001 — Backend Architecture and Coding Principles`](docs/architecture/ADR-001-backend-architecture.md)

## Temel Kodlama İlkeleri

- SOLID pragmatik biçimde uygulanır.
- YAGNI gereği mevcut ihtiyacı karşılamayan spekülatif altyapı kurulmaz.
- DRY, benzer söz dizimini değil aynı iş bilgisinin tekrarını ortadan kaldırmak için uygulanır.
- Okunabilir ve açık kod, mimari sihirden önce gelir.
- Public upload yüzeyleri güvenilmeyen giriş olarak değerlendirilir.

## Durum

Proje başlangıç aşamasındadır. İlk hedef backend foundation ve PWA iskeletinin oluşturulmasıdır.
