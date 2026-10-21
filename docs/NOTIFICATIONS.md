# Faz 7 — Hatırlatmalar ve push işleme

Faz10 worker shutdown/cancellation, ayrı `/health/workers` heartbeat ve notification result Meter instrumentation ile izlenir. Production resmi Expo endpoint/credential kullanır, fake push başarı üretemez. Hesap silme ilgili delivery/rule/device kayıtlarını transaction içinde kaldırır; in-flight kabul edilmiş push geri alınamaz. Yerel load controlled sender'dır, fiziksel teslimat kanıtı değildir. [Runbook](OPERATIONS_RUNBOOK.md), [privacy](DATA_PRIVACY.md), [checklist](PRODUCTION_CHECKLIST.md).

Yalnızca Faz 7 uygulanmıştır; .NET 8 / EF Core 8 / SQL Server ve mevcut JWT sınırı korunur. Frontend eklenmedi. Push varsayılan kapalıdır; credential yokken sahte başarı yoktur.

## HTTP sözleşmesi

Tüm uçlarda geçerli JWT ve aktif kullanıcı gerekir. Sahiplik doğrulanmış sub üzerinden belirlenir; body içine UserId koymak 400 üretir. Başka kullanıcıya ait plan/kural/cihaz 404 döner.

| Uç | Body | Başarı |
| --- | --- | --- |
| POST /api/payments/{paymentId}/reminders | ReminderRequest | 201 ReminderResponse |
| GET /api/payments/{paymentId}/reminders | — | 200 array |
| PUT /api/payments/{paymentId}/reminders/{reminderId} | ReminderRequest | 200 ReminderResponse |
| DELETE /api/payments/{paymentId}/reminders/{reminderId} | — | 204 |
| POST /api/devices | DeviceRequest | 200 DeviceResponse (upsert) |
| GET /api/devices | — | 200 array, push token içermez |
| DELETE /api/devices/{id} | — | 204 |
| GET /api/users/me/settings | — | 200 SettingsResponse |
| PUT /api/users/me/settings | SettingsRequest | 200 SettingsResponse |

ReminderRequest: `{"daysBefore":3,"timeOfDay":"09:00","isEnabled":true}`. DaysBefore zorunlu 0–365; 0 aynı gün, 1/3/7 ve özel değerler desteklenir. Saat tam HH:mm (dakika hassasiyeti); alan verilmezse 09:00. Saat kullanıcının yerel saatidir. En fazla 10 silinmemiş Payment hedefli kural/plan; Faz 8 otomatik trial kuralları ayrı hedefte tutulur. Aynı plan + Target + DaysBefore + TimeOfDay silinmemiş kayıtlar arasında benzersizdir; disabled kural da kombinasyonu ayırır. Duplicate/capacity 409, geçersiz alan 400. ReminderResponse: `{id,paymentPlanId,daysBefore,timeOfDay,isEnabled}`. Güncellemeler last-write-wins; bu uçlarda rowversion/ETag yoktur.

DeviceRequest: `{"platform":"ios","pushToken":"ExpoPushToken[<platform-token>]","id":null}`. Platform ios/android; yalnızca ExpoPushToken[...] / ExponentPushToken[...] biçimi kabul edilir. Id verilmezse kendi mevcut token kaydı yenilenir veya yeni server Id üretilir. Token değiştiğinde kendi mevcut cihaz Id değeriyle tekrar POST yapın. Aynı token başka hesaba aktarılmaz; 409 döner. Başka hesaba ait Id 404, aynı token/Id uyuşmazlığı 409. En fazla 20 silinmemiş cihaz/kullanıcı; inactive kayıt da bu bütçeye dahildir. Device identifier ayrıca tutulmaz.

DeviceResponse: `{id,platform,isActive,lastSeenAtUtc}`; token hiçbir yanıtta/logda görünmez. DELETE token değerini rastgele olmayan benzersiz `deleted:<server-id>` tombstone ile değiştirir, IsActive=false yapar, GET listesinden çıkarır ve kuyruktaki henüz kabul edilmemiş işleri iptal eder. Provider DeviceNotRegistered durumunda cihaz inactive yapılır; bu kayıt GET içinde görünür ve istemci silip yeni token ile tekrar kaydedebilir. Son görülme zamanı register/rotation isteğinde yenilenir.

SettingsRequest: `{"languageCode":"tr","defaultCurrencyCode":"TRY","timeZoneId":"Europe/Istanbul","notificationsEnabled":true}`. Dört alan gereklidir; dil tr/en, para birimi mevcut ISO 4217 listesinde, timezone işletim sisteminin .NET tarafından çözümlenebilen geçerli kimliği olmalıdır. PUT invalid timezone reddeder; legacy eksik/bozuk timezone okuması ve worker hesabı Faz 5 ile tutarlı UTC fallback kullanır. SettingsResponse aynı dört alan + `usedTimeZoneFallback`. Eksik settings okuması yazma yapmaz; UTC/tr/TRY/notifications=true varsayımlarıyla fallback=true döner. Register normal başlangıç ayarlarını Europe/Istanbul ile oluşturur.

## Zamanlama ve pencere

Recurrence engine gerçek due date üretir; occurrence satırları geleceğe topluca yazılmaz. Her etkin kural için kullanıcı timezone bilgisiyle `DueDate - DaysBefore` yerel günü + TimeOfDay hesaplanır; UTC dönüşümünde sunucu yerel saatine güvenilmez. Örnek: 24 Kasım 2026 due, 3 gün önce 09:00 Europe/Istanbul → 21 Kasım 06:00 UTC.

DST gap: yerel saat geçersizse ilk geçerli yerel dakikaya ilerle (en çok 24 saat; çözülemeyen/sınır dışı tarih atlanır). DST fold: iki UTC karşılığı varsa daha geç UTC anını seç. New York 8 Mart 2026 02:30 → 07:00 UTC; 1 Kasım 2026 01:30 → 06:30 UTC.

Bildirim zamanı için `[now-24 saat, now+30 gün]` penceresi kullanılır. Due araması local today-2 ile today+32+DaysBefore arasındadır; sonuçlar kesin UTC pencere ile filtrelenir. Böylece DaysBefore=365 olan ve bir yıl sonraya vadesi gelen ödemenin bugünkü hatırlatması kaçırılmaz. 24 saatten uzun kesinti için eski bildirimler topluca gönderilmez; vadesi geçmiş iş claim edildiğinde iptal edilir. İşlem hacmi sınırları nedeniyle tarama çevrim süresi operasyonel izlenmelidir.

Sadece aktif kullanıcı, aktif plan, silinmemiş/etkin kural, NotificationsEnabled=true ve aktif cihazlar için kayıt üretilir. Paid/Skipped atlanır; kaydı olmayan veya kayıtlı Pending uygundur. EndDate/StartDate/interval/anchor mevcut recurrence servisiyle uygulanır. Her rule/due/device için tek satır vardır; birden çok cihaz ayrı teslimattır.

Periyodik tarama pending kayıtların zamanını günceller, uygun iptal kayıtlarını tekrar değerlendirebilir; Sent/Failed/accepted ticket kayıtlarını yeniden açmaz, canlı lease ve retry backoff sıfırlamaz. Ayar/kural/token değişimi henüz kabul edilmeyen işleri SQL transaction içinde iptal eder. Plan tarih/aktiflik veya occurrence Paid/Skipped değişimi gönderimden hemen önce güncel kayıtlarla yeniden doğrulanır. Eski due satırları artık takvime ait değilse iptal edilir; yeni tarihler sonraki taramada oluşur.

Ödeme tutarı/adı bildirim payload içine alınmaz. Generic başlık/gövde ve opaque notificationId kullanılır; eski tutar sızmaz. Bildirim metni şimdilik genel Türkçe metindir; tr/en ayarı tutulur, yerelleştirilmiş şablon sistemi eklenmedi. Bildirime tıklanınca istemci güncel veriyi JWT ile API üzerinden okumalıdır.

Reminder DELETE audit/FK bütünlüğü için logical delete yapar. Mevcut Faz 2 kuralı korunur: ilişkili reminder veya occurrence geçmişi olan plan DELETE 409 alır; geçmiş zorla silinmez. Planı IsActive=false yapmak bütün gönderimleri bastırır. İlişkisi olmayan ve silinebilen plan için kuyruk kaydı bulunamaz (FK koruması).

## Kalıcı kuyruk ve worker

Application: NotificationManagementService/validator, NotificationPolicy (saf hesaplar), NotificationProcessor, INotificationQueue ve IPushNotificationSender. Infrastructure: NotificationManagementStore, SqlNotificationQueue ve Expo adapter. API: JWT controller ve scoped servis kullanan NotificationWorker BackgroundService. Genel repository/CQRS/Hangfire eklenmedi.

Worker etkinse başlangıçta ve her çevrim sonrasında varsayılan 30 saniye arayla çalışır. Çevrim en çok 20 kural tarar, en çok 50 işi sırayla işler, en çok 500 eski terminal kayıt temizler. Kural Id seek cursor ile sonraki çevrimde devam eder, son grupta başa döner. Cursor süreç ömürlüdür; yeniden başlatma baştan güvenli tarar. Çok büyük rule hacminde tarama gecikmesi artar; kapasite ve 24 saat catch-up sınırı izlenmelidir. Birden fazla instance aynı SQL kuyruğu paylaşabilir.

Zamanlama transaction plan satırını UPDLOCK/HOLDLOCK ile kilitler, unique index yarışları engeller. Claim tek `UPDATE ... OUTPUT`, UPDLOCK/READPAST/READCOMMITTEDLOCK ile atomiktir; RCSI açık veritabanlarında da locking read kullanır. LeaseOwner her claim için yeni Guid, süre 2 dakika. HTTP timeout 15 saniye. Worker kapanırsa/çökerse süresi dolan lease geri alınabilir. Completion yalnızca aynı owner ve geçerli lease için kabul edilir; geç/stale completion mevcut işi ezemez.

| Status | Anlamı |
| --- | --- |
| Pending (0) | Gönderim veya tekrar deneme bekliyor |
| Processing (1) | Lease ile sahiplenildi |
| Sent (2) | Receipt APNs/FCM aktarımını kabul etti; cihazda görüntülenme kanıtı değildir |
| Failed (3) | Kalıcı hata, deneme sınırı veya receipt belirsizliği terminal oldu |
| AwaitingReceipt (4) | Expo ticket kabul edildi; receipt bekleniyor |
| Cancelled (5) | Güncel tercih/durum/takvim nedeniyle gönderim bastırıldı |

ProviderMessageId, TicketAcceptedAtUtc, LastErrorCode (yalnızca izinli kısa kod), NextAttemptAtUtc, LeaseOwner/ExpiresAtUtc, ReceiptAttemptCount ve UpdatedAtUtc kalıcıdır. AttemptCount gönderim claim sayısıdır; çöken/geri alınan claim de sayılır. ReceiptAttemptCount ayrıdır. SentAtUtc receipt kontrolünün kabul zamanıdır. Serbest provider hata metni/response kaydedilmez. Worker yalnızca işlenen/temizlenen adetleri ve hata tipi loglar.

## Retry ve exactly-once sınırı

Gönderim hatalarında en fazla 5 claim/attempt; exponential 30, 60, 120, 240 saniye aralıkları, genel backoff tavanı 1 saat. Provider Retry-After varsa 1 saniye–1 saat sınırında dikkate alınır. HTTP 429/5xx, bağlantı/timeout ve protokol bozukluğu retryable; kalıcı credential/payload hataları Failed. DeviceNotRegistered cihazı devre dışı bırakır. Terminal Failed otomatik olarak yeniden açılmaz; nedeni düzeltilip kontrollü operasyon gerekir.

Ticket kabulü sonrası ilk receipt 15 dakika sonra; receipt yoksa/receipt sorgusu geçici hata alırsa aynı ticket tekrar sorgulanır, **push tekrar gönderilmez**. En fazla 24 saat/96 receipt claim; belirsizlik ReceiptExpired ile terminal olur. Receipt açıkça MessageRateExceeded döndürürse önceki handoff başarısızdır: ticket temizlenir ve sınırlı send retry, tekrar güncel eligibility kontrolüyle yapılır. HTTP receipt rate limitinde Retry-After kullanılır. Başarılı receipt cihazın bildirimi gösterdiği anlamına gelmez.

SQL ile dış HTTP çağrısı tek transaction değildir. Sağlayıcı kabul edip SQL completion kaybolursa lease kurtarmada tekrar push mümkün; ödeme Paid olurken çağrı yoldaysa bildirim yine ulaşabilir. Otomatik iptal lease completion sonucunu reddederse provider ticket kaydı da kaybolabilir. Süreç duraklaması/lease aşımı gibi durumlarda uçtan uca exactly-once garantisi yoktur. Kalıcı kuyruk, idempotent zamanlama ve bounded at-least-once processing vardır; cihaz teslimatı garantisi verilmez. notificationId istemcide mümkün olduğunda duplicate görünümünü azaltabilir; Expo endpoint idempotency anahtarı garantisi sunmuyor.

## Expo yapılandırması

Worker ve gerçek adapter varsayılan kapalıdır. Etkinleştirmek için Expo projesinde APNs/FCM credential ve push access-token güvenliğini hazırlayın; gerçek cihazdan üretilmiş geçerli Expo token gerekir. Mobil frontend bu fazda yoktur.

```powershell
$env:Notifications__Enabled='true'
# Notifications__AccessToken secret manager/ortam üzerinden önceden sağlanmalıdır; çıktıya yazdırmayın.
$env:Notifications__SendEndpoint='https://exp.host/--/api/v2/push/send'
$env:Notifications__ReceiptEndpoint='https://exp.host/--/api/v2/push/getReceipts'
$env:Notifications__PollSeconds='30' # 5–300
```

Etkin olup credential/HTTPS endpoint eksikse startup reddedilir. Disabled adapter manuel çağrılsa bile NotConfigured permanent hata verir, başarı üretmez. Endpoint ayarı yalnızca güvenilir deployment konfigürasyonudur, HTTP istemcisinden alınmaz. IHttpClientFactory bağlantıları yönetir; redirect kapalı, timeout 15 saniye, JSON response en fazla 64 KiB. Her istekte tek push/receipt vardır; Expo toplu ticket karmaşıklığı yoktur. Global Expo proje kotası için çok instance dağıtımında throughput/edge kontrolü ayrıca gerekir.

[Resmî Expo gönderim/receipt protokolü](https://docs.expo.dev/push-notifications/sending-notifications/) ve [teslimat anlamı](https://docs.expo.dev/push-notifications/faq/) temel alınmıştır. Ticket/receipt ayrımı testlerde HTTP handler ile, worker akışı test assembly içindeki FakePushNotificationSender ile doğrulanır. Bunlar gerçek Expo veya fiziksel cihaz teslimatı testi değildir.

## Güvenlik, logout ve veri saklama

Push token gönderim için geri okunabilmelidir; hashlemek mümkün değildir. DB plaintext alanı uygulama yanıtlarına/loglara çıkmaz; production SQL erişimi en az yetki, TLS, TDE/encrypted disk ve şifreli backup ile korunmalıdır. Encryption key/Expo/JWT credential kaynak kontrolüne konmaz. Token DELETE isteğinde alan silinir/tombstone olur. İptal edilmiş cihaz kimliği geçmiş delivery için tutulur.

Auth logout ile refresh ailesi ve cihaz arasında ilişki yoktur. **Logout tek başına push aboneliğini kapatmaz.** Mobil istemci logout öncesi kendi device Id için DELETE yapmalı, sonra auth/logout çağırmalıdır. Offline logout sonrasında eski cihaz kaydı kalabilir; kullanıcı genel NotificationsEnabled=false ile tüm gönderimleri kapatabilir. Cihaz token paylaşımı diğer hesap için otomatik takeover sağlamaz; önce mevcut kaydı silmek gerekir.

Delivery terminal kayıtları, UpdatedAtUtc ve DueDate ikisi de 400 günden eskiyse çevrimde en çok 500 silinir. Gelecek vade için dedup anahtarı erken temizlenmez. Bekleyen/receipt işleri cleanup tarafından silinmez; worker kapalı kalırsa ayrıca backlog/retention izlemesi gerekir. Deleted rule/device tombstone kayıtları FK/audit nedeniyle tutulur; kullanıcı verisi silme/anonimleştirme ve arşiv politikası ayrıca operasyonel süreçtir. Bu fazda otomatik cihaz/rule tombstone purge eklenmedi.

Tanılama için token içermeyen SQL örneği:

```sql
SELECT Status, COUNT_BIG(*) AS Jobs, MIN(NextAttemptAtUtc) AS OldestNextAttempt
FROM NotificationDeliveries GROUP BY Status;
SELECT LastErrorCode, COUNT_BIG(*) AS Jobs
FROM NotificationDeliveries WHERE LastErrorCode IS NOT NULL GROUP BY LastErrorCode;
```

Provider accept/receipt gecikmesi, Failed/Cancelled sayısı, expired lease, en eski bekleyen iş ve kural tarama süresi izlenmelidir. Worker logları token veya payload içermeden çevrim adetlerini gösterir. Deploy öncesi migration tamamlanmalı; otomatik startup migration yoktur.

## Faz 8 — Trial event hedefi

ReminderRule.Target: Payment=0 legacy, SubscriptionTrial=1. Finansal planın ilk ücretli tarihi trial bitişinden farklıdır. Trial kuralları 0/1/3/7 gün önce 09:00 kayıtlarıdır; Faz 9 1/3/7/0 önceliğiyle merkezi plan slotlarına göre açılır. Free için yeni trial yalnızca 1-gün kuralını etkinleştirir; eski etkin kurallar downgrade ile silinmez. Payment + SubscriptionTrial etkin toplamı Free 1 / yapılandırılmış Premium varsayılan 10 sınırını paylaşır. EnableTrialReminders genel tercihtir; payment API yalnızca Payment hedefi görür. DueDate=TrialEndDate; recurrence financial occurrence değildir. RuleId/Target ayrımı delivery idempotency anahtarlarını çakıştırmaz. Ayrıntı [PREMIUM.md](PREMIUM.md).

Aynı worker/queue/device/lease/retry/receipt/retention kullanılır; yeni worker yoktur. AutoRenew=false parent inactive olsa bile devam eden trial hatırlatılabilir. Cancelled/Expired/deleted/disabled trial ve kapalı user notifications gönderimi engeller; send öncesi yeniden değerlendirilir. Subscription değişikliği eski kabul edilmemiş işleri iptal eder, tarih değişimi yeni due key üretir. Eski/bitmiş trial taraması kaba tarih filtresiyle azaltılır, kesin kontrol kullanıcı timezone bilgisiyle yapılır. Tam lifecycle ve sınırlar [SUBSCRIPTIONS.md](SUBSCRIPTIONS.md) içindedir. Gerçek Expo/fiziksel cihaz teslimatı bu fazda doğrulanmadı.
