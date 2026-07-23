# 007 — Outbox Locking ve Multi-Worker Güvenliği

## Amaç

Aynı outbox mesajının iki farklı uygulama instance'ı veya worker tarafından aynı anda teslim edilmesini engellemek.

## Önceki risk

Önceki processor uygun mesajları normal bir `SELECT` ile okuyordu. İki worker aynı anda çalıştığında her ikisi de aynı `ProcessedAtUtc = null` kayıtlarını okuyabilir ve aynı notification, SSE veya gelecekteki email/push teslimatını iki kez gerçekleştirebilirdi.

## Çözüm

`OutboxMessages` tablosuna iki nullable alan eklendi:

- `LockId`
- `LockedAtUtc`

Her processor çalışması benzersiz bir `LockId` üretir. Aday mesajlar önce okunur, ardından her kayıt koşullu ve atomik bir `UPDATE` ile sahiplenilir.

Bir kayıt ancak aşağıdaki şartları hâlâ sağlıyorsa claim edilebilir:

- İşlenmemiş olması
- Retry limitini aşmamış olması
- Sonraki deneme zamanının gelmiş olması
- Kilitsiz olması veya kilidinin zaman aşımına uğramış olması

Koşullu update yalnızca bir satır etkilediğinde mesaj ilgili worker tarafından sahiplenilmiş kabul edilir.

## Lock timeout

Varsayılan lock timeout 300 saniyedir.

Worker mesajı claim ettikten sonra çökerse kayıt sonsuza kadar kilitli kalmaz. `LockedAtUtc` timeout süresini geçtiğinde başka bir worker mesajı yeniden claim edebilir.

## Teslimat sonrası

Başarılı teslimatta:

- `ProcessedAtUtc` atanır
- `NextAttemptAtUtc` temizlenir
- `LastError` temizlenir
- Lock alanları temizlenir

Başarısız teslimatta:

- `RetryCount` artırılır
- `LastError` kaydedilir
- Exponential backoff ile `NextAttemptAtUtc` hesaplanır
- Lock alanları temizlenir

Her mesajdan sonra ayrı `SaveChangesAsync` çağrısı yapılır. Böylece batch içindeki daha önce başarıyla işlenen mesajlar, sonraki bir mesajın hatası nedeniyle kaybedilmez.

## Teslimat garantisi

Bu tasarım eşzamanlı duplicate delivery riskini engeller ve `at-least-once` teslimat modeli sağlar.

Harici kanal çağrısı başarılı olduktan hemen sonra uygulama çöker ve `ProcessedAtUtc` kaydedilemezse aynı mesaj timeout sonrası yeniden teslim edilebilir. Bu nedenle email, push ve webhook gibi harici kanalların da mesaj ID'sini idempotency anahtarı olarak kullanması önerilir.

## Migration

`20260718113000_AddOutboxLocking`

Eklenen indexler:

- `IX_OutboxMessages_LockId`
- `IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_LockedAtUtc_OccurredAtUtc`

## Operasyonel öneriler

- `LockTimeoutSeconds`, en uzun normal teslimat süresinden daha büyük olmalıdır.
- Uzun süren kanal teslimatları için ileride lock renewal/heartbeat eklenmelidir.
- Lock timeout nedeniyle tekrar claim edilen mesajlar log ve metric ile izlenmelidir.
- Harici teslimat kanalları mümkün olduğunca idempotent olmalıdır.
