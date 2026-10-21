# Operasyon runbook'u

Production'da komut çalıştırılmadı. İşlemler hedef/kimlik/backup/onay kontrolünden sonra yetkili operatöre yöneliktir. Yerel scriptler yalnızca kendi rastgele DB/container'ını temizler; geliştirme volume'u korunur.

## Sağlık / metrik / alarm

Live200 süreç; ready200 SQL ve migration güncel;503 SQL/pending migration. `/health/workers`: etkin worker'ın son başarılı döngüsü10dk eski/yoksa503; ilk döngü öncesi503 olabilir. Worker endpoint'i readiness'e dahil değildir; mağaza/Expo outage bütün CRUD'u kapatmaz. Heartbeat kuyruk boşalması veya cihaz teslimatı değildir.

Meter `PayAhead`: request/status/duration(ms), authentication401/403, rate rejected/unavailable, worker success/failure. `PayAhead.Processing`: notification result ve webhook reconciled/unbound/retry/terminal. ActivitySource PayAhead + ASP.NET Activity trace; X-Correlation-ID max64ASCII alnum/-/_, aksi trace ID üretilir. Serilog CorrelationId taşır. Label'larda user/token/email/path yoktur. Built-in instrumentation hazır, kalıcı OTLP/Prometheus exporter/collector kurulmadı; staging kurulumu Pending. Diagnostics kapalı container'da in-process exporter veya onaylı collector değişikliği gerekir; kullanıcıya açık admin/metrics ucu yoktur.

Alarm: ready503>1dk, worker>10dk, rate503 artışı, Failed delivery/receipt, storeAttemptCount>=10, oldest pending age, deadlock/timeout/pool, CPU/memory/restart. Hata category/code inceleyin; password/token/body loglamayın. Proxy/APM Google outbound URL path'indeki token'ı da redakte etmelidir.

## Ortak rate limit

SQL RateLimitBuckets + transaction application lock + UPDLOCK/HOLDLOCK tüm instance'larda ortak. SQL SYSUTCDATETIME pencere saatidir. Auth login/register/refresh/logout20/dk/IP; verify/restore10/dk/user (anonimseIP); webhook60/dk/IP; export/delete5/dk/user. JWT key ile domain-separated HMAC; düz IP/user/token saklanmaz. Aynı JWT key/SQL gerekir; key rotasyonu kısa pencereyi sıfırlar. Local limiter ek koruma olarak kalır, distributed katman SQL'dir.

SQL limiter5s deadline içinde çalışmazsa sensitive route503 fail-closed; liveness/rate dışı okumalar bu middleware'den etkilenmez.429 Retry-After60. NAT müşterileri ortak IP bütçesi kullanır. Çok IP counter cardinality/DDoS için edge WAF/global ingress koruması gerekir. Redis eklenmedi; mevcut SQL transaction altyapısı seçildi.

## Worker / lease / poison / manuel retry

Notification20rule/50job/cycle,2dk lease,5send-attempt, receipt/backoff ve400gün terminal cleanup. Store1inbox/call,2dk lease,10attempt,30s→1h backoff;10sonrası terminal inceleme. Notification completion lease owner/süre fencing kullanır. Store son kullanıcı kilidi altında güncel provider state'i yeniden sorgular. Çöken process'in lease'i bitince başka instance claim eder. Cancellation SQL/dış çağrıya iletilir;30s host/40s container grace önerilir. Belirsiz dış sonuç+retry duplicate etki üretebilir; exactly-once yoktur.

```sql
SELECT Id,Store,AttemptCount,ErrorCode,NextAttemptAtUtc,LeaseExpiresAtUtc
FROM StoreWebhookInbox WHERE CompletedAtUtc IS NULL AND AttemptCount>=10;
```

Token/body seçmeyin. Credential/IAM/network/sertifika sorununu düzeltip UUID'yi onaylayın. `ops/retry-webhook.sql` completed olmayan10attempt ve expired/no lease satırını requeue eder. sqlcmd InboxId yalnızca trusted operator UUID'sidir; otomasyon SqlParameter kullanmalıdır (SQL testinde doğrulandı). Completed/Unbound körlemesine açılmaz, unknown replacement verify/restore gerektirir. Notification terminal durumunda accepted ticket/belirsiz gönderim/dedup incelenmeden tekrar Pending yapılmaz.

## Retention / SQL

Maintenance varsayılan açık, dakikada bir, varsayılan200/batch. RefreshRetentionDays120 (90–3650), BatchSize1–1000. Expiry ve revoke cutoff'tan eski ve ailesinde retention içinde token bulunmayan zincirler temizlenir. Self-FK incoming referansı olmayan eski başlar önce; uzun zincir sonraki batch'lerde tamamlanır. Aktif session/reuse detection geçmişi korunur. UPDLOCK/READPAST ve idempotent SQL delete multi-instance güvenliği sağlar. Rate bucket expiry+5dk sonrası ayrı bounded batch; aktif quota silinmez. Store audit/inbox otomatik purge yok, privacy/retention onayı Pending.

Runtime hesabı DDL/backup yetkili olmaz, migration ayrı kimlikle. EF command timeout30s, transient retry3; işlemler execution strategy içinde transaction tekrarıyla. Temel READ COMMITTED, policy writes User UPDLOCK/HOLDLOCK, export SERIALIZABLE, inbox READPAST/READCOMMITTEDLOCK. RCSI/snapshot ayrı staging testidir. Deadlock1205 transient retry; sürekli ise lock order/index/query plan incelenir. Pooling varsayılan açık/MaxPoolSize100; instance×pool toplamını SQL kapasitesine göre sınırlayın.

Migration idempotent script review + doğrulanmış backup + maintenance window sonrası ayrı adım; pending migration ready503. Yeni ProductionOperations yalnızca rate tablosu/index ekler. Otomatik Down yok; rollback önceki binary/schema uyumu, ileri düzeltme veya restore planıdır. Büyük değişikliklerde expand/contract.

Backup RPO/RTO ve recovery modeline göre full/diff/log zinciri; şifreli disk/object storage ve ayrı key/access alanı. Migration öncesi COPY_ONLY+CHECKSUM normal zinciri etkilemez. VERIFYONLY tek başına yetmez: ayrı hedefe MOVE ile gerçek restore, CHECKDB, counts ve API smoke gerekir. Canlı DB üstüne WITH REPLACE çalıştırmayın. Yerel script iki kendi DB'siyle bu tatbikatı geçti; production şifreleme/RPO/RTO doğrulanmadı.

Query Store, index usage/physical stats ve pahalı query planlarını ölçün. Otomatik tüm-index rebuild yok; statistics/reorganize/rebuild bakım penceresinde log/disk etkisiyle planlanır. Export/silme/cleanup lock ve log yükü staging'de ölçülmelidir. Kısa burst yerel load, sustained/soak veya production SLO kanıtı değildir.
