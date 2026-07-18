# 013 — Release Verification

## Amaç

Bu belge production hardening çalışmalarından sonra release kararının hangi çalıştırılmış kanıtlara dayanacağını tanımlar.

## Kod tarafında tamamlanan doğrulama altyapısı

- Backend CI tek bir build/test job'u olarak sadeleştirildi.
- CI PostgreSQL 17 service container kullanır.
- Release build `--warnaserror` ile çalışır.
- Unit ve integration testleri çalıştırılır.
- Tüm EF Core migration'ları temiz PostgreSQL veritabanına uygulanır.
- `dotnet ef migrations has-pending-model-changes` ile model/snapshot tutarlılığı doğrulanır.
- EF Core CLI işlemleri için `OrtakareDbContextFactory` eklendi.
- CI diagnostic logları artifact olarak saklanır.

## Mevcut dış blocker

Son GitHub Actions çalışmaları job step'lerine başlamadan başarısız olmuştur. Job step listesinin boş olması, workflow içindeki restore/build/test komutlarının çalıştırılmadığını gösterir. Bu durum repository kodundan önce GitHub-hosted runner tahsisi, Actions kullanım kotası veya ödeme ayarı seviyesinde incelenmelidir.

Bu dış blocker çözülmeden aşağıdaki maddeler doğrulanmış sayılamaz:

- NuGet restore
- Release build
- Unit testler
- Integration testler
- Temiz PostgreSQL migration uygulaması
- EF snapshot/model consistency

## GitHub tarafında yapılacak işlem

Repository veya organization için:

1. Settings → Billing and licensing → Actions usage kontrol edilir.
2. Spending limit veya ödeme yöntemi hatası varsa düzeltilir.
3. Actions'ın repository için enabled olduğu doğrulanır.
4. Backend CI workflow yeniden çalıştırılır.

## Yerel doğrulama komutları

```bash
cd backend

dotnet restore Ortakare.slnx

dotnet build Ortakare.slnx \
  --configuration Release \
  --no-restore \
  --warnaserror

dotnet test Ortakare.slnx \
  --configuration Release \
  --no-build
```

Temiz PostgreSQL üzerinde:

```bash
export ConnectionStrings__PostgreSql='Host=localhost;Port=5432;Database=ortakare_release_check;Username=postgres;Password=postgres'

dotnet ef database update \
  --project src/Ortakare.Api/Ortakare.Api.csproj \
  --startup-project src/Ortakare.Api/Ortakare.Api.csproj \
  --configuration Release \
  --no-build

dotnet ef migrations has-pending-model-changes \
  --project src/Ortakare.Api/Ortakare.Api.csproj \
  --startup-project src/Ortakare.Api/Ortakare.Api.csproj \
  --configuration Release \
  --no-build
```

## Başarısız snapshot kontrolü halinde

`has-pending-model-changes` başarısız olursa elle snapshot düzenlenmez.

Geliştirme ortamında:

```bash
dotnet ef migrations add SynchronizeModelSnapshot \
  --project src/Ortakare.Api/Ortakare.Api.csproj \
  --startup-project src/Ortakare.Api/Ortakare.Api.csproj
```

Oluşan migration incelenir.

- Beklenmeyen tablo silme veya yeniden oluşturma varsa migration commit edilmez.
- Yalnızca beklenen model/index farkları varsa migration ve snapshot birlikte commit edilir.
- Ardından temiz PostgreSQL migration testi yeniden çalıştırılır.

## GO şartı

Aşağıdaki kanıtların tamamı oluşmadan PR ready-for-review veya production GO yapılmamalıdır:

- Backend CI conclusion: success
- Release build: success
- Unit tests: success
- Integration tests: success
- Clean PostgreSQL migration: success
- Pending model changes: none
- Production secrets: configured outside repository
- Backup/restore rehearsal: completed
- `/health/live`: 200
- `/health/ready`: 200
- Upload/export smoke tests: success
