# Ortakare API Production Configuration

## Container modeli

API multi-stage Docker build ile derlenir. SDK yalnızca build aşamasında kullanılır; production image `aspnet:10.0` runtime image'ıdır.

Container root kullanıcı ile çalışmaz. Uygulama `ortakare` system user altında `8080` portunu dinler.

## Secret yönetimi

Production secret değerleri image içine, `appsettings.json` dosyasına veya Git repository'sine yazılmaz.

`.env.example` yalnızca gerekli environment variable isimlerini gösterir. Gerçek `.env` dosyası Git'e commit edilmez.

Aşağıdaki değerler deployment ortamında secret olarak verilmelidir:

- `ConnectionStrings__PostgreSql`
- `Jwt__SigningKey`
- `ObjectStorage__AccessKey`
- `ObjectStorage__SecretKey`
- `Hangfire__Dashboard__Username` ve `Hangfire__Dashboard__Password` dashboard açılacaksa
- `POSTGRES_PASSWORD` compose ile PostgreSQL çalıştırılacaksa

## Başlatma

```bash
cp .env.example .env
```

`.env` içindeki `CHANGE_ME` değerleri güçlü ve benzersiz secret değerlerle değiştirilmelidir.

```bash
docker compose --env-file .env -f docker-compose.production.yml up -d --build
```

## Ağ erişimi

Compose yapılandırmasında API host üzerinde yalnızca `127.0.0.1:8080` adresine bind edilir. Public HTTPS trafiği reverse proxy üzerinden gelmelidir.

PostgreSQL için host port publish edilmez. Veritabanı yalnızca `ortakare` Docker network'ü içinde erişilebilir durumdadır.

## Health check

Uygulamanın ayakta olduğunu kontrol etmek için:

```text
/health/application
```

PostgreSQL ve R2 dependency kontrolü:

```text
/health/dependencies
```

Container image içine sırf healthcheck için `curl` veya `wget` eklenmedi. Runtime image minimal tutulur; container health probe deployment platformu veya reverse proxy tarafından `/health/application` üzerinden yapılmalıdır.

## Production kontrol listesi

- Gerçek secret değerleri `.env` veya deployment secret store üzerinden verildi.
- `Cors__AllowedOrigins` yalnızca gerçek PWA origin'lerini içeriyor.
- Reverse proxy HTTPS terminate ediyor.
- Forwarded headers yalnızca güvenilen proxy üzerinden kabul ediliyor.
- `/health/dependencies` public internetten gerekiyorsa proxy seviyesinde sınırlandırıldı.
- Hangfire Dashboard varsayılan olarak kapalı tutuldu.
- PostgreSQL public host portuna açılmadı.
