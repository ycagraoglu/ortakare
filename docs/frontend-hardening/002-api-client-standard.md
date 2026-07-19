# 002 — API Client Standardı

## Amaç

Frontend ile ASP.NET Core backend arasındaki bütün HTTP iletişimini tek, tip güvenli ve test edilebilir bir katmanda toplamak.

## Backend sözleşmesi

Backend başarı ve hata yanıtlarında aşağıdaki zarfı kullanır:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": null,
  "data": {}
}
```

Feature ve component katmanları bu zarfı doğrudan kullanmaz. API katmanı başarılı yanıtta yalnızca `T`, başarısız yanıtta `ApiError` döndürür.

## Eklenen yapı

```text
src/shared/api/
├── api-error.ts
├── api-result.ts
├── auth-bridge.ts
├── axios.ts
├── correlation-id.ts
├── http.ts
├── index.ts
├── unwrap-api-result.ts
└── unwrap-api-result.test.ts
```

## Merkezi Axios client

- Base URL yalnızca doğrulanmış `VITE_API_URL` değerinden alınır.
- Varsayılan timeout 30 saniyedir.
- Bütün isteklerde `Accept: application/json` kullanılır.
- Her isteğe yeni `X-Correlation-Id` eklenir.
- Access token yalnızca authentication bridge üzerinden alınır.
- Component veya feature içinde ayrı Axios instance oluşturulmaz.

## Hata sınıflandırması

`ApiError.kind` aşağıdaki değerlerden biridir:

- `aborted`
- `network`
- `timeout`
- `unauthorized`
- `forbidden`
- `not-found`
- `conflict`
- `validation`
- `rate-limit`
- `server`
- `unknown`

Backend mesajı mevcutsa korunur. Backend mesajı yoksa kullanıcıya uygun güvenli varsayılan Türkçe mesaj kullanılır.

## Authentication sınırı

002 kapsamında refresh-token uygulanmamıştır. Bunun yerine 003 Authentication Session maddesinin bağlanacağı iki nokta hazırlanmıştır:

```ts
configureApiAuthentication({
  getAccessToken,
  onUnauthorized,
});
```

Böylece API katmanı token storage veya auth state kütüphanesine doğrudan bağımlı değildir.

## Kullanım standardı

Feature API dosyası:

```ts
import { apiGet } from "@/shared/api";

export function getMyEvents(signal?: AbortSignal): Promise<EventSummary[]> {
  return apiGet<EventSummary[]>("/api/events", { signal });
}
```

TanStack Query:

```ts
queryFn: ({ signal }) => getMyEvents(signal)
```

Component tarafı `AxiosResponse`, `ApiResult<T>` veya `response.data.data` görmez.

## Cancellation

Bütün typed helper fonksiyonları `AbortSignal` kabul eder. TanStack Query'nin sağladığı signal doğrudan API fonksiyonuna aktarılmalıdır.

## Retry kararı

Axios seviyesinde otomatik retry yoktur. Retry politikası 004 TanStack Query Standardı kapsamında, yalnızca güvenli ve idempotent sorgular için merkezi olarak belirlenecektir.

## Doğrulama

Eklenen unit testler:

- başarılı `ApiResult<T>` içinden data çıkarılması
- başarısız backend sonucunda `ApiError` fırlatılması
- bozuk response sözleşmesinin reddedilmesi
- datasız başarılı yanıtın kabul edilmesi

Dosyalar ve testler branch'e eklenmiştir. `npm install`, typecheck, lint, test ve build henüz çalıştırılmadığı için çalışma zamanı başarısı iddia edilmez.
