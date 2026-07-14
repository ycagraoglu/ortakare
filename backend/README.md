# Ortakare Backend

Ortakare backend, ASP.NET Core 10 LTS tabanlı tek bir Web API projesidir.

Bağlayıcı mimari kararlar:

- [`ADR-001 — Backend Architecture and Coding Principles`](../docs/architecture/ADR-001-backend-architecture.md)

## Yapı

```text
backend
├── Ortakare.slnx
├── Directory.Build.props
├── src
│   └── Ortakare.Api
│       ├── Features
│       ├── Common
│       ├── Infrastructure
│       │   └── Persistence
│       ├── Middleware
│       ├── Extensions
│       └── Program.cs
└── tests
    ├── Ortakare.UnitTests
    └── Ortakare.IntegrationTests
```

`Infrastructure` burada ayrı mimari katman veya ayrı proje değildir. Tek API projesi içindeki PostgreSQL, Cloudflare R2, e-posta ve background job gibi teknik uygulamaları gruplandıran klasördür.

## Temel Kurallar

- Feature-first Vertical Slice Architecture kullanılır.
- MediatR ve Minimal API kullanılmaz.
- Controller'lar yalnızca HTTP giriş noktasıdır.
- Handler'lar controller constructor'ından inject edilir.
- EF Core doğrudan ilgili slice handler'ında kullanılabilir.
- Generic repository kullanılmaz.
- Ayrı Domain, Application veya Infrastructure projeleri oluşturulmaz.
- SOLID, YAGNI ve DRY pragmatik biçimde uygulanır.
