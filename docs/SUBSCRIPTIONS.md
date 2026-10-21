# Faz 8 — Harici abonelik ve ücretsiz deneme takibi

Bu belge Netflix/Spotify/iCloud gibi **kullanıcının beyan ettiği harici abonelikleri** anlatır. Bunlar TrackedSubscription'dır ve Premium sağlamaz. Faz 9 PayAhead mağaza üyeliği ayrı UserSubscription üzerinden doğrulanır; [PREMIUM.md](PREMIUM.md) ve [STORE_INTEGRATION.md](STORE_INTEGRATION.md) sözleşmeleri geçerlidir. Yeni aktif harici abonelik Free 3, Premium yapılandırılmış limit (varsayılan 1000) ile sınırlıdır; bağlı aktif plan ayrıca plan limitini kullanır.

## Model ve tek finansal kaynak

Yeni TrackedSubscription: Id, UserId, PaymentPlanId, ProviderName, servis StartDate, TrialStartDate/TrialEndDate, beyan edilen Status, AutoRenew, EnableTrialReminders, CancellationRequestedAtUtc/CancelledAtUtc, AccessEndsOn, IsDeleted, CreatedAtUtc/UpdatedAtUtc ve SQL rowversion.

Her aboneliğin yalnızca kendisine ait yeni bir PaymentPlan kaydı vardır. Name/Description/Amount/CurrencyCode/BillingFrequency/BillingInterval/AnchorDay bu planda tutulur; abonelik tablosunda tekrar edilmez. **PaymentPlan.StartDate ilk ücretli tahsilat tarihi FirstBillingDate demektir; subscription.StartDate servis başlangıcıdır.** NextBillingDate mevcut recurrence servisiyle kullanıcının bugünü referans alınarak hesaplanır; kalıcı ikinci tarih alanı yoktur.

Amount, trial sonrası beklenen ücret olup pozitiftir ve decimal(19,4) sınırındadır; ücretsiz trial için Amount=0 girilmez. Amount otomatik Paid anlamına gelmez. FirstBillingDate zorunlu, servis başlangıcında/sonrasında ve varsa TrialEndDate değerinden **kesin olarak sonra** olmalıdır. Trial bitiş günü dahil deneme sayılır; ilk ücret aynı gün varsayılmaz. Aradaki ücretsiz boşluk açıkça temsil edilebilir.

One-to-one unique PaymentPlanId index ve (PaymentPlanId,UserId) composite FK farklı kullanıcının planıyla ilişkiyi engeller. İstemci PaymentPlanId/UserId gönderemez, önceden var olan planı bağlayamaz; POST atomik yeni abonelik+plan+trial rule oluşturur. İsim/provider aynı olan ayrı aboneliklere izin verilir; bunlar kullanıcının ayrı takip kayıtlarıdır. İlişki sonradan başka plana taşınmaz.

Bağlı plan payment GET/list/occurrence/reminder altyapısında görünür. Generic payment PUT/DELETE 409 döner; metadata subscription API üzerinden yönetilir. Böylece ayrı plan güncellemesiyle abonelik durumu veya concurrency sınırı atlanamaz. Ödeme durumu Paid/Skipped/Pending yine mevcut occurrence API üzerinden takip edilir.

Dashboard/upcoming/calendar yalnızca PaymentPlan verisini toplar; TrackedSubscription ikinci kez toplamlara katılmaz. Trial sırasında AutoRenew=true ise gelecekteki ilk ücretli tarihten başlayan yenilemeler görünür; trial bitiş event'i finansal tahsilat değildir. AutoRenew=false ise plan pasiftir, gelecekte ücret tahmin edilmez. Kayıtlı Paid/Skipped geçmişi mevcut inactive-plan merge politikasıyla okunur.

## API

Tüm uçlar geçerli JWT ve aktif kullanıcı gerektirir. Sahiplik doğrulanmış sub üzerinden gelir; başka hesaba ait kaynak 404, anonim erişim 401, unknown body alanları 400 ProblemDetails.

| İşlem | Başarı |
| --- | --- |
| POST /api/subscriptions | 201 SubscriptionResponse, Location/ETag |
| GET /api/subscriptions | 200 paginated result |
| GET /api/subscriptions/{id} | 200 SubscriptionResponse, ETag |
| PUT /api/subscriptions/{id} | 200 SubscriptionResponse, yeni ETag |
| DELETE /api/subscriptions/{id} | 204, güçlü If-Match gerekir |
| POST /api/subscriptions/{id}/cancel | 200 SubscriptionResponse, yeni ETag |
| GET /api/subscriptions/trials/expiring?withinDays=7 | 200 array |
| GET /api/subscriptions/summary | 200 SubscriptionSummary |

Create örneği (tarihleri kendi senaryonuza göre seçin):

```json
{
  "name": "Spotify",
  "description": "Kişisel müzik aboneliği",
  "providerName": "Spotify",
  "amount": 99.99,
  "currencyCode": "TRY",
  "billingFrequency": "Monthly",
  "billingInterval": 1,
  "startDate": "2026-11-01",
  "trialStartDate": "2026-11-01",
  "trialEndDate": "2026-11-30",
  "firstBillingDate": "2026-12-02",
  "status": "Trial",
  "autoRenew": true,
  "enableTrialReminders": true
}
```

Name 1–150, Description en fazla2000, ProviderName 1–100; text trim edilir. CurrencyCode mevcut ISO4217 snapshot üzerinden doğrulanır/uppercase saklanır. Monthly/Yearly/Weekly/Daily, pozitif BillingInterval ve isteğe bağlı AnchorDay 1–31 desteklenir. Once kabul edilmez. Trial tarihleri birlikte ve servis başlangıcında/sonrasında sıralı olmalıdır. Trial için tarihler zorunludur; Active kayıtta geçmiş trial tarihleri korunabilir veya hiç olmayabilir. AutoRenew=true AccessEndsOn verilirse ilk ücretli tarihten önce olamaz. Nonrenewing Active için AccessEndsOn zorunludur. DateOnly default/min tarih servis başlangıcı olarak reddedilir.

PUT aynı body alanlarını ve **son response.rowVersion** değerini ister; create rowVersion kabul etmez. DELETE `If-Match: "<rowVersion>"`; weak/multiple/wildcard desteklenmez. Cancel body:

```json
{"accessEndsOn":"2026-12-31","rowVersion":"<son-response-rowVersion>"}
```

Cancel AccessEndsOn bugün veya sonrası ve servis başlangıcından önce olmayacak şekilde girilmelidir. Geçersiz alan/biçim/geçiş 400, stale version veya terminal kaydı güncelleme 409. Tekrarlanan cancel idempotent başarı değildir; terminal kaynak/stale version 409 döner. İptal edilmiş/süresi dolmuş kayıt silinebilir; yeniden etkinleştirme yerine yeni bir takip kaydı oluşturulur.

Liste query: Status, ProviderName (case-insensitive tam eşleşme), CurrencyCode, Search (name/provider üzerinde case-insensitive contains), PageNumber(default1), PageSize(default20,max100). Sıralama CreatedAtUtc descending, sonra Guid ascending; bütün isteklerde deterministik. PageNumber 1–1.000.000. Kullanıcı başına en fazla1000 silinmemiş abonelik; projection/list/summary bu bounded veri kümesinde hesaplanır. Limit aşımında sessiz eksik toplam dönmez. Liste `{items,pageNumber,pageSize,totalCount,totalPages}`.

SubscriptionResponse yukarıdaki alanlara ek olarak Id/PaymentPlanId, türetilmiş Status/NextBillingDate/TrialDaysRemaining, UTC audit timestamp'leri, RowVersion ve `{today,timeZoneId,usedTimeZoneFallback}` context içerir. RowVersion subscription sürümüdür, parent plan sürümü değildir.

## Yaşam döngüsü ve iptal

Write Status yalnızca Trial veya Active olabilir. Trial→Active açık kullanıcı güncellemesiyle mümkündür; Active→Trial yasaktır. Zaman kaynaklı durum GET/list/summary sırasında, kullanıcının yerel bugünü üzerinden türetilir; GET veritabanına durum yazmaz.

| Durum | Kural |
| --- | --- |
| Trial | Başlangıçta beyan edilen trial; end günü dahil |
| Trial→Active | TrialEndDate sonrası AutoRenew=true; ücretin ödendiği varsayılmaz |
| Trial→Expired | TrialEndDate sonrası AutoRenew=false |
| Trial/Active→Cancelled | Cancel endpoint; AutoRenew=false, plan inactive |
| Active/Cancelled→Expired | AccessEndsOn geçtikten sonra |

AccessEndsOn dahil erişim sürdüğü kabul edilir; sonraki gün Expired. TrialStartDate gelecekteyse kayıt beyanen Trial görünür fakat expiring listesine/gönderime henüz girmez. Süresi geçmiş bir trial geçmiş kayıt olarak oluşturulabilir; response o günün EffectiveStatus değerini döndürür. Nonrenewing trial AccessEndsOn verilmemişse TrialEndDate sonunda biter.

CancellationRequestedAtUtc ve CancelledAtUtc yerel takipte iptal beyanının kaydedildiği aynı server UTC anıdır; harici sağlayıcıdan teyit değildir. AccessEndsOn ayrı tarihtir. AutoRenew=false gelecek ücret tahminlerini kapatır; önceden tahsil edilmiş erişim geleceğe kadar sürebilir. Harici aboneliği gerçekten iptal etmek kullanıcının sağlayıcıda yapacağı işlemdir; bu API sağlayıcı hesabına bağlanmaz.

DELETE logical delete yapar; linked plan inactive kalır, subscription listeden kaybolur, tüm kabul edilmemiş bildirim işleri iptal edilir. PaymentPlan/PaymentOccurrence/accepted ticket geçmişi silinmez. Generic payment API üzerinden archived plan yeniden açılamaz. Mevcut Faz5 tarihsel tutar/para birimi snapshot sınırlaması değişmedi: geçmişteki kaydın metadata'sı güncel parent plandan okunur.

## Trial günü ve saat dilimi

UserSettings.TimeZoneId, TimeProvider UTC anından yerel bugünü üretir; eksik/invalid zone UTC fallback ve context bayrağıdır. PUT settings invalid timezone reddi mevcut Faz7 politikasını korur. TrialDaysRemaining = max(0, TrialEndDate.DayNumber - Today.DayNumber); end günü 0 döner, negatif değer yoktur. Bu tam gün farkıdır, saat sayısı veya yuvarlanmış 24 saat değildir.

Expiring endpoint WithinDays varsayılan7,0–365. Yalnızca effective Trial, TrialStartDate<=today ve kalan gün<=WithinDays döner; end günü dahil, geçmiş/Cancelled/Expired hariç. Response: subscriptionId,name,providerName,trialEndDate,daysRemaining,firstBillingDate,amount,currencyCode. İlk ücretli tarih trial sonrası açık tarihtir; AutoRenew=false olsa da olası ücret bilgisini gösterir, gelecekte tahsilat planlamaz.

## Tahmini aylık karşılık

Summary: Total/Active/Trial/Cancelled/ExpiredSubscriptionCount, MonthlyEquivalentTotalsByCurrency, ExpiringTrialsCount (7 gün), NextRenewal (en erken next date, sonra Id) ve context. Silinmiş kayıtlar dahil değildir; cancelled erişim bitince Expired sayılır.

Yalnızca effective Active/Trial ve AutoRenew=true olan gelecek maliyet tahminleri dahil edilir. Her para birimi ayrı gruptur. Aşağıdaki tutarlar tahmin olup dashboard gerçek due-date tahsilat toplamıyla karıştırılmamalıdır:

| Sıklık | Aylık karşılık |
| --- | --- |
| Monthly | Amount / BillingInterval |
| Yearly | Amount / (12 × BillingInterval) |
| Weekly | Amount × 52 / (12 × BillingInterval) |
| Daily | Amount × 365 / (12 × BillingInterval) |

Sabit52 hafta/365 gün yıllıklaştırma kullanılır; takvim ayı uzunluğu/leap yılına göre gerçek faturalar farklı olabilir. Decimal ara değerler satır başında yuvarlanmaz; currency group toplamı4 haneye MidpointRounding.ToEven yuvarlanır. İç toplam alanı `estimatedAmount` adını taşır. Kurlar arasında dönüşüm veya para birimlerini birleştirme yoktur.

## Mevcut worker ile trial reminder

Yeni trial worker/provider/queue oluşturulmadı. ReminderRule.Target: Payment=0 (legacy default), SubscriptionTrial=1. Trial tarihleri olan abonelikte dört 0/1/3/7 gün önce 09:00 rule atomik oluşturulur; Faz 9 merkezi slot limitine göre 1/3/7/0 önceliğiyle etkinleştirilir. Free tek 1-gün kuralı, Premium yeterli slotla dört kural açar. EnableTrialReminders genel aç/kapat alanıdır; downgrade mevcut etkin kuralları silmez. Payment ve trial aktif kuralları aynı plan limitini paylaşır. Generic payment reminders API yalnızca Payment hedefini yönetir. Unique index (PaymentPlanId,Target,DaysBefore,TimeOfDay), delivery dedup (RuleId,DueDate,DeviceId).

Trial event DueDate, TrialEndDate değeridir; finansal recurrence günü değildir. Saat/timezone/DST dönüşümü,30gün ileri pencere,24saat catch-up, device/preferences, lease, retry, receipt ve retention Faz7 altyapısıyla ortaktır. Trial scheduler ve send revalidation halen geçerli Trial/TrialEnd/TrialStart/reminder tercihini kontrol eder; Cancelled/Expired/deleted/manual Active trial göndermez. Billing plan AutoRenew=false diye inactive olsa da devam eden trial bitişi için uyarı gönderilebilir.

Subscription update/cancel/delete aynı transaction içinde henüz provider ticket almamış işleri iptal eder. Tarih değişirse yeni event key oluşur; eski due tekrar gönderilmez. NotificationsEnabled=false/devices inactive ise gönderim olmaz. Coarse UTC date filtresi eski bitmiş trial rule taramalarını azaltır; kesin eligibility yine kullanıcı timezone'unda kontrol edilir. Bildirim metni halen genel PayAhead metnidir; frontend opaque notificationId ile güncel bilgiyi API'den okumalıdır. Provider ticket/receipt fiziksel cihaz teslimatı değildir; dış sistem yarışları ve exactly-once sınırlaması [NOTIFICATIONS.md](NOTIFICATIONS.md) içinde aynen geçerlidir.

## Transaction, migration ve doğrulama

Plan-first UPDLOCK/HOLDLOCK sırası scheduler ile paylaşılır; subscription+plan+rules+queue cancellation tek EF execution-strategy transaction'ıdır. Güncelleme/cancel/delete rowversion karşılaştırır; eşzamanlı işlemlerden yalnızca biri commit eder. Enjekte edilen SaveChanges hatasında plan/rule/queue iptali rollback ile korunur. Create yeni parent ürettiği için mevcut plan sahipliğini client değiştiremez; SQL unique/composite FK ikinci katmandır.

Yeni `20261021132035_ExternalSubscriptionTracking`:11. tablo TrackedSubscriptions, status/trial/access checks, unique plan link, ownership FK ve indexes; ReminderRule.Target default0 ve genişleyen unique index. Up veri/kolon silmez; mevcut Premium UserSubscriptions ve payment/reminder/delivery kayıtlarını yeniden amaçlandırmaz. Eski migration dosyaları korunur; startup migrate etmez. Down yeni abonelik verisini düşürür ve eski rule uniqueness ile çakışabilir; otomatik production rollback yapmayın, backup/ileri düzeltme planlayın.

Testler gerçek SQL Server Testcontainers kullanır. Unit lifecycle/validation/timezone/recurrence/forecast ve SQL schema metadata; integration CRUD/summary/ownership/concurrency/rollback/upgrade/worker fake akışını doğrular. Kestrel kendi geçici DB ve gerçek JWT ile denenir. **Gerçek Expo/fiziksel push teslimatı doğrulanmadı.** Banka/provider entegrasyonu, otomatik gerçek harici abonelik iptali ve frontend kapsam dışıdır. Faz 9 üyelik doğrulaması bu harici aboneliklerden ayrıdır.
