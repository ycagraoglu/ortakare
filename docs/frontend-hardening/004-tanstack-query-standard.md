# 004 — TanStack Query Standardı

## Amaç

Ortakare frontend uygulamasında server state yönetimini feature bazlı, tahmin edilebilir ve güvenli bir standarda bağlamak.

## Temel kararlar

- Server state yalnızca TanStack Query ile yönetilir.
- Query key değerleri feature içinde factory ile üretilir.
- Component içinde rastgele string query key yazılmaz.
- Mutation işlemleri otomatik retry edilmez.
- Yalnızca geçici GET hataları sınırlı sayıda retry edilir.
- Logout ve kullanıcı değişiminde bütün server cache temizlenir.
- Query function, TanStack Query tarafından verilen `AbortSignal` değerini API katmanına iletir.

## Query key standardı

Her feature kendi scope değerini tanımlar:

```ts
export const eventKeys = createQueryKeyFactory("events");
```

Üretilen hiyerarşi:

```text
["events"]
["events", "list"]
["events", "list", filters]
["events", "detail"]
["events", "detail", eventId]
```

Bu yapı sayesinde liste, detay veya feature kapsamındaki bütün sorgular kontrollü biçimde invalidate edilebilir.

## Query kullanımı

```ts
export function useEvents(filters: EventFilters) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: ({ signal }) => eventApi.getList(filters, signal),
  });
}
```

## Mutation kullanımı

```ts
export function useCreateEvent() {
  return useMutation({
    mutationFn: eventApi.create,
    onSuccess: async () => {
      await invalidateQueries(eventKeys.lists());
    },
  });
}
```

Mutation sonrasında bütün cache'i temizlemek yerine yalnızca etkilenen query ailesi invalidate edilir.

## Retry politikası

Aşağıdaki hata türleri en fazla iki retry alır:

- network
- timeout
- server

Aşağıdaki hatalar retry edilmez:

- unauthorized
- forbidden
- not-found
- conflict
- validation
- rate-limit
- aborted
- unknown

Gecikme üstel olarak artar ve 8 saniye ile sınırlandırılır.

## Auth sınırı

Logout, session expiration ve farklı kullanıcıyla login sırasında:

1. Aktif query istekleri iptal edilir.
2. Query ve mutation cache temizlenir.
3. Auth snapshot güncellenir.

Böylece önceki kullanıcıya ait server state yeni oturumda görünmez.

## Varsayılan süreler

```text
staleTime: 30 saniye
gcTime: 5 dakika
refetchOnWindowFocus: false
refetchOnReconnect: true
```

Feature ihtiyaçları bu değerlerden farklıysa query tanımında açıkça override edilmelidir.

## Doğrulama

Eklenen unit testler:

- geçici hata retry davranışı
- kalıcı hata retry engeli
- retry gecikme üst sınırı
- query key hiyerarşisi

Repository üzerinde henüz `npm ci`, typecheck, lint, test ve build çalıştırılmadığı için çalışma sonucu doğrulanmış kabul edilmez.
