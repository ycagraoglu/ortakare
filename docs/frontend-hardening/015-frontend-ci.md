# 015 — Frontend CI

## Amaç

Frontend kalite kontrollerini tek ve tekrarlanabilir bir komutta toplamak; GitHub Actions kullanılamadığında dahi PR öncesi aynı kalite kapısının yerelde çalıştırılmasını sağlamak.

## Mevcut kısıt

GitHub Actions hesabın ödeme/kota durumu nedeniyle otomatik olarak çalışmıyor. Bu nedenle bu adımın ana kalite kapısı GitHub Actions değil, repository içinde tanımlı yerel `verify` komutudur.

Workflow dosyası yalnızca `workflow_dispatch` ile elle çalıştırılabilir durumdadır. `push` ve `pull_request` trigger'ları bilerek eklenmemiştir; böylece ücret/kota tüketen otomatik koşular oluşturulmaz.

## Tek komut kalite kapısı

Frontend klasöründe:

```bash
npm run verify
```

sırasıyla şunları çalıştırır:

```text
typecheck
→ lint
→ test + coverage
→ production build
→ bundle budget
```

Komutlardan biri başarısız olursa zincir durur ve PR hazır kabul edilmez.

## Scriptler

```json
{
  "check:bundle": "node ./scripts/check-bundle-budget.mjs",
  "verify": "npm run typecheck && npm run lint && npm run test:coverage && npm run build && npm run check:bundle"
}
```

## Bundle budget

Başlangıç bütçeleri:

```text
Toplam JavaScript: 500 KB
Tek JavaScript dosyası: 350 KB
Tek CSS dosyası: 120 KB
```

Bu değerler raw build çıktısı üzerinden kontrol edilir. Gzip/Brotli bütçesi production readiness aşamasında ayrıca değerlendirilecektir.

Bütçe aşılırsa script hangi asset'in limiti geçtiğini göstererek non-zero exit code döndürür.

## GitHub Actions workflow

Dosya:

```text
.github/workflows/frontend-ci.yml
```

Yalnızca elle tetiklenebilir:

```yaml
on:
  workflow_dispatch:
```

Workflow kullanılabilir hale geldiğinde:

```text
checkout
→ Node.js 22
→ npm ci
→ npm run verify
→ dist ve coverage artifact
```

akışını uygular.

## Lockfile zorunluluğu

Şu anda `frontend/package-lock.json` repository'de bulunmamaktadır. Yeni test dependency'leri eklendiği için lockfile gerçek npm kurulumu ile üretilmelidir:

```bash
cd frontend
npm install
```

Ardından oluşturulan `package-lock.json` commit edilmelidir.

Lockfile olmadan:

```bash
npm ci
```

çalışmaz. Bu nedenle manuel workflow da lockfile eklenene kadar başarılı olamaz.

## PR öncesi manuel kontrol

GitHub Actions kullanılamadığı sürece geliştirici aşağıdaki sırayı uygulamalıdır:

```bash
cd frontend
npm install
npm run verify
```

PR açıklamasında aşağıdakiler açıkça belirtilmelidir:

```text
[ ] npm run typecheck geçti
[ ] npm run lint geçti
[ ] npm run test:coverage geçti
[ ] npm run build geçti
[ ] npm run check:bundle geçti
```

Çalıştırılmayan kontrol başarılı işaretlenmemelidir.

## Branch protection sınırı

Actions çalışmadığı için GitHub üzerinde required status check zorunluluğu uygulanamaz. Bu dönemde koruma süreci teknik değil operasyoneldir:

```text
PR aç
→ yerelde npm run verify çalıştır
→ sonucu PR açıklamasına yaz
→ review tamamlanmadan merge etme
```

Actions yeniden kullanılabilir olduğunda `Frontend CI / Verify frontend` check'i branch protection altında required yapılmalıdır.

## Artifact politikası

Manuel workflow çalıştırıldığında şu çıktılar 7 gün tutulur:

```text
frontend/dist
frontend/coverage
```

Uzun süreli release artifact saklama bu workflow'un sorumluluğu değildir.

## Doğrulama durumu

Repository'ye script, bundle budget ve manuel workflow eklendi. Ancak şu komutlar bu çalışma sırasında çalıştırılmadı:

```bash
npm install
npm run verify
```

Dolayısıyla lockfile, dependency uyumu, testler, coverage, build ve bundle budget henüz doğrulanmış değildir.
