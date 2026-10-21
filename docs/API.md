# API sözleşmesi — Faz 1–10

## Faz 10 — Privacy ve işletim

JWT GET `/api/users/me/export`: kendi profil/settings/plan/occurrence/reminder/tracked/membership/device metadata JSON attachment, no-store. Secret/token/hash ve başka kullanıcı verisi yok. Bounded satırlar ve8MiB çıktı; aşım413. DELETE `/api/users/me`: `{"currentPassword":"..."}`, tekrar parola doğrulaması,4096byte limit; başarı204, yanlış parola401. Sonraki eski JWT401. UserId body/query üzerinden seçilemez. Geçmiş silinir, store anti-replay bağları pseudonymous tutulur; mağaza auto-renew iptal olmaz. Tam sözleşme [DATA_PRIVACY.md](DATA_PRIVACY.md).

Privacy ortak5/dakika/user; auth20/dakika/IP; verify/restore10/dakika/user; webhook60/dakika/IP SQL-backed distributed limit.429 Retry-After60, limiter outage503 fail-closed. Genel body1MiB; endpoint özel daha küçük limitler korunur. Oversize413 ProblemDetails, sensitive alanlar hata yanıtında tekrarlanmaz. CORS exact HTTPS allowlist, credentials yok. X-Correlation-ID, nosniff/DENY/no-referrer/Permissions-Policy başlıkları eklenir. Live/ready yanında anonim minimal GET `/health/workers` vardır; ready dış provider outage'a bağlı değildir. [Runbook](OPERATIONS_RUNBOOK.md).

Faz 9 üyelik uçları: anonim GET `/api/premium/plans`; JWT ile GET `/api/premium/me`, GET `/api/premium/entitlements`, POST `/api/premium/verify-purchase`, POST `/api/premium/restore`. Son iki uç `{store,productId,evidence}` kabul eder; Apple JWS veya Google purchaseToken, sunucuda doğrulanır. 200 entitlement, 400 geçersiz kanıt, 401 kimlik, 409 hesap bağlama, 429 rate limit, 503 sağlayıcı/config hatası. Ayrıntılı alanlar ve React Native sözleşmesi [PREMIUM.md](PREMIUM.md).

POST `/api/webhooks/apple/subscriptions` ve `/api/webhooks/google/subscriptions` JWT politikası dışında olsa da mağaza JWS / Google OIDC imzası zorunludur; başarılı inbox kabulü 202. Güvenlik ve konfigürasyon [STORE_INTEGRATION.md](STORE_INTEGRATION.md). Premium cevapları no-store'dur. Yeni aktif payment/reminder/subscription veya aktifleştirme güncel 5/1/3 Free / yapılandırılmış Premium limitini aşarsa 403 ProblemDetails, `code=entitlement_limit`; mevcut etkin kayıtlar downgrade ile silinmez.

| Uç | Başarı | Hata | Kimlik |
| --- | --- | --- | --- |
| GET /health/live | 200, `{ "status": "Healthy" }` | — | Anonim |
| GET /health/ready | 200, `{ "status": "Healthy" }` | 503 ProblemDetails | Anonim |
| GET /swagger/v1/swagger.json | 200 OpenAPI | Development dışında yok | Yalnızca Development |

Readiness SQL bağlantısı ve uygulanmamış migration kontrolü yapar. Genel hata gövdesi application/problem+json kullanır; traceId tanılama içindir. 500 yanıtı iç hata mesajı veya stack trace içermez. Ödeme uçlarının tamamı kimlik doğrulama ister. Domain/EF entity'leri yerine DTO döner; yanıtlarda UserId yoktur.

## POST /api/payments

```http
POST /api/payments
Content-Type: application/json
```

```json
{
  "name": "İnternet faturası",
  "description": "Ev bağlantısı",
  "amount": 499.9000,
  "currencyCode": "TRY",
  "category": "Bills",
  "frequency": "Monthly",
  "interval": 1,
  "startDate": "2026-10-31",
  "endDate": null,
  "anchorDay": 31,
  "isActive": true
}
```

Örnek yanıt (değerler temsilidir): **201 Created**, `Location: /api/payments/11111111-1111-4111-8111-111111111111`, `ETag: "AAAAAAAAAAE="`.

```json
{"id":"11111111-1111-4111-8111-111111111111","name":"İnternet faturası","description":"Ev bağlantısı","amount":499.9000,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","interval":1,"startDate":"2026-10-31","endDate":null,"anchorDay":31,"isActive":true,"createdAtUtc":"2026-10-21T12:00:00Z","updatedAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAE="}
```

Zorunlu alanlar: name, amount, currencyCode, category, frequency, interval, startDate. Name boş/whitespace olamaz ve en fazla 150 karakterdir. Description en fazla 2000, category boş olmayan en fazla 50 karakterdir. Tutar > 0, en fazla 999999999999999.9999 ve en fazla dört anlamlı ondalık basamak olmalıdır; sessiz yuvarlama yapılmaz. CurrencyCode [SIX ISO 4217 güncel liste snapshot'ı](https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html) ile doğrulanır (yayın 2026-09-17; fon/özel ISO kodları dahil). Başında/sonunda boşluk ve küçük harf kabul edilip invariant uppercase olarak saklanır. Eski tarihsel kodlar bu listede yoksa reddedilir; mevcut kayıtlara otomatik dönüşüm yapılmaz.

Frequency yalnızca string enum: Once, Daily, Weekly, Monthly, Yearly. Sayısal enum kabul edilmez. Interval >= 1 (Int32 sınırında); başlangıç tarihi zorunlu ve 0001-01-01 olamaz; EndDate varsa başlangıçtan önce olamaz. AnchorDay isteğe bağlı 1–31; IsActive varsayılan true. Bu fazda yalnızca sıklık bilgisi saklanır; occurrence veya recurrence hesabı yapılmaz.

Name, description ve category trim edilir. Id, UserId ve zaman damgalarını sunucu belirler. Kullanıcı kaydı var ve aktif olmalıdır. UserId/CreatedAtUtc gibi sözleşme dışı body alanları 400 ile reddedilir. Kimlik HTTP kullanıcı başlığından alınmaz.

## GET /api/payments

Örnek istek:

```http
GET /api/payments?pageNumber=1&pageSize=20&isActive=true&category=Bills&currencyCode=TRY&search=fatura
```

Örnek **200 OK**:

```json
{"items":[{"id":"11111111-1111-4111-8111-111111111111","name":"İnternet faturası","description":"Ev bağlantısı","amount":499.9000,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","interval":1,"startDate":"2026-10-31","endDate":null,"anchorDay":31,"isActive":true,"createdAtUtc":"2026-10-21T12:00:00Z","updatedAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAE="}],"pageNumber":1,"pageSize":20,"totalCount":1,"totalPages":1}
```

PageNumber varsayılan 1, aralık 1–1.000.000; PageSize varsayılan 20, aralık 1–100. Sınır ihlali 400 döner; sessiz clamp yapılmaz. Filtreler AND ile birleşir. IsActive belirtilmezse aktif/pasif tüm sahip olunan planlar listelenir. Category tam eşleşme, CurrencyCode normalize edilmiş kod eşleşmesi, search Name içinde literal alt metin aramasıdır. Metin karşılaştırmalarının harf/aksan duyarlılığını SQL Server collation belirler. Search en fazla 150 karakter; boş/whitespace filtreler reddedilir. `%`, `_` gibi karakterler wildcard değildir; EF parametreli sorgu kullanır.

Sıralama CreatedAtUtc DESC, ardından Id ASC (SQL Server sırası). Liste sorgusu AsNoTracking kullanır; count ve sayfa için iki sorgu çalışır, N+1 yoktur. Eşzamanlı ekleme/silmede count/sayfa tek snapshot garantisi vermez. Sıfır sonuçta totalPages 0; aralık dışındaki geçerli sayfada items boş, totalCount gerçek toplamdır.

## GET /api/payments/{id}

```http
GET /api/payments/11111111-1111-4111-8111-111111111111
```

Örnek **200 OK**, `ETag: "AAAAAAAAAAE="`:

```json
{"id":"11111111-1111-4111-8111-111111111111","name":"İnternet faturası","description":"Ev bağlantısı","amount":499.9000,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","interval":1,"startDate":"2026-10-31","endDate":null,"anchorDay":31,"isActive":true,"createdAtUtc":"2026-10-21T12:00:00Z","updatedAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAE="}
```

Bulunmayan, Guid.Empty veya başka kullanıcıya ait kayıt aynı 404 yanıtını üretir. Biçimi bozuk GUID 400 döner. Tüm sorgular doğrulanmış kullanıcı Id'siyle sınırlandırılır.

## PUT /api/payments/{id}

Tam alan güncellemesidir; POST kurallarına ek olarak **rowVersion zorunludur**. Son GET/POST/PUT yanıtındaki sekiz baytlık canonical Base64 SQL rowversion body içinde gönderilir. PATCH semantiği yoktur; örneğin belirtilmeyen description null, IsActive varsayılan true olur.

```http
PUT /api/payments/11111111-1111-4111-8111-111111111111
Content-Type: application/json
```

```json
{"name":"İnternet faturası","description":"Yeni tarife","amount":599.9000,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","interval":1,"startDate":"2026-10-31","endDate":null,"anchorDay":31,"isActive":true,"rowVersion":"AAAAAAAAAAE="}
```

Örnek **200 OK**, `ETag: "AAAAAAAAAAI="`:

```json
{"id":"11111111-1111-4111-8111-111111111111","name":"İnternet faturası","description":"Yeni tarife","amount":599.9000,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","interval":1,"startDate":"2026-10-31","endDate":null,"anchorDay":31,"isActive":true,"createdAtUtc":"2026-10-21T12:00:00Z","updatedAtUtc":"2026-10-21T12:05:00Z","rowVersion":"AAAAAAAAAAI="}
```

Id/UserId/CreatedAtUtc değişmez; UpdatedAtUtc sunucu UTC saatinden gelir. SQL UPDATE özgün rowversion ile koşulludur; iki yazardan yalnızca biri başarılı olur. İçerik aynı olsa da sürüm veritabanında kontrol edilir. Eksik/bozuk token 400, eski token veya eşzamanlı silme 409 döner. Çakışmada güncel kaydı tekrar okuyup değişiklikleri kullanıcıya gösterin; kör tekrar yapmayın.

## DELETE /api/payments/{id}

```http
DELETE /api/payments/11111111-1111-4111-8111-111111111111
If-Match: "AAAAAAAAAAI="
```

Başarılı yanıt: **204 No Content**, response body yoktur. GET/POST/PUT yanıtındaki ETag başlığı aynen gönderilir. Tek strong, çift tırnaklı Base64 ETag zorunludur. Eksik/bozuk token, `*`, weak veya çoklu ETag 400; eski token bu API sözleşmesinde 409 üretir.

Silme stratejisi **ilişkisi olmayan plan için hard delete**: PaymentOccurrence veya ReminderRule varsa 409 döner, plan ve ilişkili veriler korunur. NotificationDelivery zaten ReminderRule üzerinden bağımlıdır. Kontrolden sonra eklenen ilişkiler de SQL FK tarafından korunur; 547 FK hatası güvenli 409'a çevrilir. Tek SaveChanges atomiktir; ilgili geçmiş kayıtlarına cascade uygulanmaz. Soft delete eklenmedi: IsActive pasif/aktif iş durumudur, silinme göstergesi değildir. Geçmişi olan planı `isActive=false` ile pasifleştirebilirsiniz; geçmişi otomatik temizleme yoktur.

## Hatalar ve kimlik

| Durum | HTTP |
| --- | --- |
| Eksik/bozuk JSON, validation, GUID veya concurrency token biçimi | 400 |
| Kimlik yok; oluşturma sırasında olmayan/pasif kullanıcı | 401 |
| Kayıt yok veya başka kullanıcıya ait | 404 |
| Eski rowversion, eşzamanlı değişiklik, ilişkili kayıtla silme | 409 |
| Beklenmeyen hata | 500 |

Örnek doğrulama yanıtı (RFC ProblemDetails, traceId temsilidir):

```json
{"title":"İstek doğrulanamadı.","status":400,"errors":{"amount":["Tutar pozitif, decimal(19,4) sınırlarında ve en fazla dört ondalıklı olmalıdır."]},"traceId":"request-trace-id"}
```

API model binding ve FluentValidation hataları application/problem+json döner. İç hata mesajları, SQL veya stack trace döndürülmez. Kullanıcı eşleştirmesi ve sorgu filtreleri sunucudadır. Faz 6 ile gerçek JWT aktiftir; DevelopmentIdentity API içinde kimlik sağlamaz. Auth sözleşmesi aşağıdadır. Domain recurrence servisi Faz 5 okuma uçlarında kullanılır; geleceğe otomatik occurrence kaydı oluşturulmaz.

## PUT /api/payments/{paymentId}/occurrences/{dueDate}/status

Bir planın yalnızca belirtilen tarihindeki durumunu kaydeder. Başarılı yeni kayıt, güncelleme ve tekrar istekleri **200 OK** döner. DueDate URL'de `yyyy-MM-dd` olarak gönderilir. Paid için **ActualAmount zorunludur**; plan tutarı varsayılan alınmaz. Tutar pozitif decimal(19,4) olmalı; sıfır, negatif, taşan veya dört anlamlı ondalıktan fazla değer 400 döner. Pending/Skipped için ActualAmount null veya eksik olmalıdır.

Yeni, geçerli tarihte örnek istek:

```http
PUT /api/payments/11111111-1111-4111-8111-111111111111/occurrences/2026-01-31/status
Content-Type: application/json
```

```json
{"status":"Paid","actualAmount":299.99}
```

Örnek 200 yanıtı (Id/token/saat temsilidir):

```json
{"id":"22222222-2222-4222-8222-222222222222","paymentPlanId":"11111111-1111-4111-8111-111111111111","dueDate":"2026-01-31","status":"Paid","actualAmount":299.99,"paidAtUtc":"2026-10-21T12:00:00Z","createdAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAE=","isRecorded":true}
```

Farklı duruma veya farklı Paid tutarına geçmek için **occurrence'ın son rowVersion'ı** gerekir; parent PaymentPlan token'ını kullanmayın. Eksik/eski token 409; biçimi bozuk token 400 döner. Yeni kayıt oluştururken rowVersion null/eksik olmalıdır; var olmayan kayda dolu token ile Paid/Skipped gönderilmesi 409'dur.

```json
{"status":"Skipped","rowVersion":"AAAAAAAAAAE="}
```

Örnek 200 yanıtı:

```json
{"id":"22222222-2222-4222-8222-222222222222","paymentPlanId":"11111111-1111-4111-8111-111111111111","dueDate":"2026-01-31","status":"Skipped","actualAmount":null,"paidAtUtc":null,"createdAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAI=","isRecorded":true}
```

Pending'e dönüş:

```json
{"status":"Pending","rowVersion":"AAAAAAAAAAI="}
```

Örnek 200 yanıtı:

```json
{"id":null,"paymentPlanId":"11111111-1111-4111-8111-111111111111","dueDate":"2026-01-31","status":"Pending","actualAmount":null,"paidAtUtc":null,"createdAtUtc":null,"rowVersion":null,"isRecorded":false}
```

Pending'e dönüşte kayıt transaction içinde **silinir**. Hiç kayıt yokken Pending isteği veya reset'in tekrarı veri yazmadan aynı sanal Pending yanıtını üretir. Aynı Paid/Skipped durum+tutar tekrarı, geçerli biçimdeki token eski veya eksik olsa da yazma yapmadan mevcut kaydı döndürür; PaidAtUtc/CreatedAtUtc/rowversion değişmez. Paid tutarı değiştirilirse güncel token gerekir ve PaidAtUtc yeni kayıt işleminin UTC zamanını alır. Status yalnızca Pending/Paid/Skipped string enum olabilir; sayısal body enum kabul edilmez. UserId, PaidAtUtc ve sözleşme dışı alanlar gönderilemez.

| Geçiş | Sonuç |
| --- | --- |
| Sanal Pending → Paid | Kayıt oluşturulur, gerçek tutar ve sunucu PaidAtUtc yazılır |
| Sanal Pending → Skipped | Kayıt oluşturulur; tutar ve PaidAtUtc null |
| Paid → Skipped | Aynı Id/CreatedAtUtc korunur; tutar ve PaidAtUtc temizlenir |
| Skipped → Paid | Aynı kayda pozitif tutar ve sunucu PaidAtUtc yazılır |
| Paid/Skipped → Pending | Kayıt silinir; sanal Pending döner |
| Paid/Skipped → aynı durum+tutar | Salt okuma; aynı yanıt ve token |

Yazmadan önce tarih mevcut recurrence servisiyle doğrulanır: `GetNextOccurrence(plan, dueDate) == dueDate`. 31 Ocak aylık planda 28 Şubat geçerli, 27 Şubat geçersizdir. StartDate öncesi/EndDate sonrası veya takvime uymayan tarih 400 döner. Gerçekleşme tarihinin geçmişte/gelecekte olması tek başına engel değildir. Yeni tarih hesabı algoritması eklenmedi.

**Pasif plan politikası:** kayıtlı geçmiş okunabilir; bütün status PUT istekleri, aynı durumu tekrar gönderme dahil, 409 döner. Plan takvimi sonradan değiştirilmişse eski kayıtlar okunabilir ama artık güncel takvime uymayan tarihe yazma 400 döner. Plan silinmiş/yok/başka kullanıcıya aitse üç uç da bilgi sızdırmadan 404 üretir.

## GET /api/payments/{paymentId}/occurrences

Bu uç **yalnızca veritabanında kayıtlı durumları** döndürür; hesaplanan bütün Pending tarihlerini üretmez. Normal yazma akışında Paid ve Skipped kayıtları bulunur; önceden kalmış Pending kayıtları varsa bunlar da filtreye göre görülebilir, otomatik silinmez. Hesaplanan takvim görünümü Faz 5 kapsamındadır.

```http
GET /api/payments/11111111-1111-4111-8111-111111111111/occurrences?fromDate=2026-01-01&toDate=2026-12-31&status=Paid&pageNumber=1&pageSize=20
```

Örnek **200 OK**:

```json
{"items":[{"id":"22222222-2222-4222-8222-222222222222","paymentPlanId":"11111111-1111-4111-8111-111111111111","dueDate":"2026-01-31","status":"Paid","actualAmount":299.99,"paidAtUtc":"2026-10-21T12:00:00Z","createdAtUtc":"2026-10-21T12:00:00Z","rowVersion":"AAAAAAAAAAE=","isRecorded":true}],"pageNumber":1,"pageSize":20,"totalCount":1,"totalPages":1}
```

FromDate/ToDate isteğe bağlı, sınırlar dahil; FromDate>ToDate 400. Status isteğe bağlı tanımlı enum değeridir. Sayfa varsayılanı 1/20, PageSize 1–100 ve PageNumber 1–1.000.000; hatalı değerler 400. Sıra DueDate DESC, Id ASC. Sahiplik kontrolü, count ve page için sabit üç sorgu kullanılır; N+1 yoktur. Eşzamanlı yazmalarda count/page tek snapshot garantisi vermez. Pasif plan veya takvimi değişmiş planın kayıtlı geçmişi bu uçtan okunur.

## GET /api/payments/{paymentId}/occurrences/{dueDate}

```http
GET /api/payments/11111111-1111-4111-8111-111111111111/occurrences/2026-02-28
```

Kayıt varsa kaydedilmiş DTO, yoksa **aktif planın geçerli recurrence tarihinde** sanal Pending DTO döner. Örnek **200 OK**:

```json
{"id":null,"paymentPlanId":"11111111-1111-4111-8111-111111111111","dueDate":"2026-02-28","status":"Pending","actualAmount":null,"paidAtUtc":null,"createdAtUtc":null,"rowVersion":null,"isRecorded":false}
```

GET veri yazmaz. Geçersiz tarih Pending gibi gösterilmez: bozuk takvim tarihi/model binding 400; aktif plan takvimine uymayan tarih 400. Pasif planda kayıt yoksa sanal Pending üretilmez, 404 döner. Kayıtlı geçmiş, takvim değişmiş veya plan pasif olsa da gösterilir.

## Occurrence concurrency ve geçmiş sınırları

SQL Server transaction'ı plan satırını UPDLOCK/HOLDLOCK ile korur. Tarih doğrulaması, mevcut occurrence okuması ve insert/update/delete aynı Serializable transaction içindedir. Eşzamanlı plan değişikliği/silme bu işlemle sıralanır. Aynı plan/tarihe iki Paid isteği tek kayıt ve aynı yanıt; çakışan farklı durumlar 200/409 üretir. Unique(PaymentPlanId,DueDate), FK ve occurrence rowversion korunur; SQL 2601/2627/547 kullanıcıya ham hata olarak verilmez, 409'a çevrilir. Diğer beklenmeyen hatalar ayrıntısız 500 ProblemDetails olur. Retry destekli EF execution strategy tüm transaction'ı kapsar.

Bu bir **son durum geçmişidir**, append-only audit ledger değildir. Pending'e dönüş önceki durum, tutar, PaidAtUtc, Id ve CreatedAtUtc bilgisini kayıttan kaldırır; ayrı audit olayı yazılmaz. Sonradan tekrar Paid yapılırsa yeni Id/CreatedAtUtc oluşur. Idempotence ardışık aynı durum istekleri içindir; silme/reset sonrasında çok geç gelen eski, tokensız oluşturma isteğini yeni istekten ayıran kalıcı idempotency anahtarı yoktur. İstemci akışında güncel durumu okuyun ve değişikliklerde dönen occurrence rowVersion'ı kullanın. Plan adı/para birimi snapshot'ı occurrence'ta tutulmaz; finansal audit/kur değişimi geçmişi bu fazda sağlanmaz.

## Faz 5 — Ortak okuma sözleşmesi

Üç yeni uç salt okunurdur ve doğrulanmış kullanıcıya özeldir; UserId query/body'den alınmaz. Aktif planların geçerli tarihleri Domain recurrence servisiyle hesaplanır. Kayıt yoksa Pending, kayıt varsa kayıtlı status kullanılır. Plan+tarih anahtarıyla tekilleştirme yapılır. Kayıtlı Paid/Skipped tarihleri plan pasif olsa veya güncel takvim dışında kalsa da istenen DueDate aralığında korunur. Pasif plandan yeni Pending üretilmez; pasif planın eski Pending marker'ları gösterilmez.

`ProjectedPayment`: paymentPlanId, paymentName, dueDate, amount, currencyCode, category, frequency, status, actualAmount, paidAtUtc. Paid için **amount=ActualAmount**; Pending/Skipped için amount güncel plan tutarıdır. Skipped tutarı sadece bilgi amaçlıdır, borç toplamına girmez. Timestamp'ler UTC/Z, takvim tarihleri DateOnly'dır. Bu DTO occurrence rowVersion taşımaz; mevcut durumu değiştirmeden önce Faz 4 detay GET'inden token alın.

Her response `context` içerir: fromDate, toDate, today, timeZoneId, usedTimeZoneFallback. Yerel bugün UserSettings.TimeZoneId ve TimeProvider.GetUtcNow ile hesaplanır. Eksik/boş/geçersiz/desteklenmeyen timezone'da UTC ve usedTimeZoneFallback=true kullanılır; sunucu yerel timezone'u kullanılmaz. Varsayılan fromDate yerel bugün; toDate=fromDate+30, iki sınır da dahil olduğundan **31 takvim tarihi** içerir. 7 günlük tam pencere için toDate=fromDate+6, 30 günlük tam pencere için +29 gönderin. DateOnly.MaxValue yakınında varsayılan bitiş MaxValue'da durur. Yalnızca toDate gönderip bugünden önce bir tarih seçerseniz 400 döner; geçmiş için iki sınırı da verin.

Örnekler sabit tarihli test verileridir: iki aktif plan (A TRY: 10.1234 TRY, B USD: 2.50 USD), 31 Ocak TRY kaydı Paid/7.4321, USD kaydı Skipped; 28 Şubat ikisi de Pending. Örnek context.today=2026-01-01 olarak varsayılmıştır; canlı yanıtta kullanıcı saatine göre değişir.

## GET /api/payments/upcoming

```http
GET /api/payments/upcoming?fromDate=2026-01-01&toDate=2026-02-28&status=Pending&currencyCode=USD&pageNumber=1&pageSize=20
```

Örnek **200 OK**:

```json
{"context":{"fromDate":"2026-01-01","toDate":"2026-02-28","today":"2026-01-01","timeZoneId":"UTC","usedTimeZoneFallback":false},"items":[{"paymentPlanId":"22222222-2222-4222-8222-222222222222","paymentName":"B USD","dueDate":"2026-02-28","amount":2.50,"currencyCode":"USD","category":"Bills","frequency":"Monthly","status":"Pending","actualAmount":null,"paidAtUtc":null}],"pageNumber":1,"pageSize":20,"totalCount":1,"totalPages":1}
```

Status varsayılan Pending; Paid veya Skipped ile kayıtlı durumlar da okunabilir. CurrencyCode isteğe bağlı güncel ISO 4217 kodudur, trim/uppercase edilir. Aralık açıkça geçmişi içeriyorsa Pending filtresi gecikmiş borçları da döndürebilir. Sıra DueDate ASC → PaymentName **ordinal** ASC → PaymentPlanId (.NET Guid sırası) ASC; kullanıcı kültürü/collation sıralamayı değiştirmez. Sayfalama birleşim/filtre/sıralamadan sonra yapılır. PageNumber varsayılan 1 (1–1.000.000), PageSize varsayılan 20 (1–100). Boş listede totalPages=0; geçerli ama boş sayfada totalCount korunur.

## GET /api/dashboard/summary

```http
GET /api/dashboard/summary?fromDate=2026-01-01&toDate=2026-02-28
```

Örnek **200 OK**:

```json
{"context":{"fromDate":"2026-01-01","toDate":"2026-02-28","today":"2026-01-01","timeZoneId":"UTC","usedTimeZoneFallback":false},"activePaymentPlanCount":2,"upcomingPaymentCount":2,"paidPaymentCount":1,"skippedPaymentCount":1,"pendingPaymentCount":2,"upcomingTotalsByCurrency":[{"currencyCode":"TRY","amount":10.1234},{"currencyCode":"USD","amount":2.50}],"paidTotalsByCurrency":[{"currencyCode":"TRY","amount":7.4321}],"nextUpcomingPayment":{"paymentPlanId":"11111111-1111-4111-8111-111111111111","paymentName":"A TRY","dueDate":"2026-02-28","amount":10.1234,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","status":"Pending","actualAmount":null,"paidAtUtc":null}}
```

- ActivePaymentPlanCount kullanıcıdaki **IsActive=true** plan sayısıdır; tarih aralığına veya EndDate'nin geçmiş olmasına göre değişmez.
- PendingPaymentCount istenen aralıktaki bütün Pending tarihleri sayar; gecikmiş olanlar dahildir.
- UpcomingPaymentCount bunun DueDate>=context.today alt kümesidir. Varsayılan aralıkta sayılar aynı olabilir; geçmiş seçilince anlamları ayrılır.
- UpcomingTotalsByCurrency yalnızca bu yaklaşan Pending alt kümesinin **plan tutarlarını** toplar; Paid/Skipped/gecikmiş Pending dahil değildir.
- PaidTotalsByCurrency aralıktaki Paid kayıtlarının **ActualAmount** toplamıdır. Paid/Skipped sayımının aralığı PaidAtUtc değil DueDate'ye göredir.
- NextUpcomingPayment aynı aralıktaki yaklaşan Pending'lerin ilkidir; yoksa null. Sınırsız geleceği taramaz.
- Para birimleri ordinal kod sırasıyla ayrı döner; sıfır gruplar eklenmez. Hiçbir para birimi dönüşümü/tek ortak para toplamı yapılmaz; finansal hesaplamalar decimal'dır.

## GET /api/calendar

```http
GET /api/calendar?year=2026&month=2
```

Örnek **200 OK**:

```json
{"year":2026,"month":2,"context":{"fromDate":"2026-02-01","toDate":"2026-02-28","today":"2026-01-01","timeZoneId":"UTC","usedTimeZoneFallback":false},"days":[{"date":"2026-02-28","paymentCount":2,"totalsByCurrency":[{"currencyCode":"TRY","amount":10.1234},{"currencyCode":"USD","amount":2.50}],"payments":[{"paymentPlanId":"11111111-1111-4111-8111-111111111111","paymentName":"A TRY","dueDate":"2026-02-28","amount":10.1234,"currencyCode":"TRY","category":"Bills","frequency":"Monthly","status":"Pending","actualAmount":null,"paidAtUtc":null},{"paymentPlanId":"22222222-2222-4222-8222-222222222222","paymentName":"B USD","dueDate":"2026-02-28","amount":2.50,"currencyCode":"USD","category":"Bills","frequency":"Monthly","status":"Pending","actualAmount":null,"paidAtUtc":null}]}]}
```

Year (1–9999) ve month (1–12) zorunludur. Status verilmezse bütün durumlar; isteğe bağlı status/currencyCode filtreleri uygulanabilir. Yalnızca seçilen ayın ödeme içeren günleri döner; boş günler üretilmez. Günler artan tarihle, gün içi ödemeler ortak deterministik sırayla döner. PaymentCount filtrelenmiş günün bütün durumlarını sayar. **TotalsByCurrency o günün yalnızca Pending tutarlarını** içerir; geçmiş ay seçildiyse gecikmiş Pending de bu günlük borç tutarına girer. Paid/Skipped satırları görünür fakat borca eklenmez; sadece Paid filtresinde totals boş olabilir. Takvim aylık olduğu için ayrı pagination yoktur; ortak sonuç sınırı uygulanır.

## Okuma limitleri ve tarihsel sınırlar

En fazla **366 dahil gün**, **1.000 ilgili plan**, **50.000 kayıtlı kaynak occurrence** ve **50.000 birleşik plan/tarih** okunabilir/hesaplanabilir. Sınır aşımı 400 ProblemDetails'tır; kısmi veya sessiz kırpılmış toplam dönmez. Sonuç sınırı status filtresi ve pagination öncesi uygulanır. Dar tarih/para birimi seçerek isteği küçültün. Domain'in plan başına 10.000 sınırı da korunur; 366 günlük pencere günlük planı en fazla 366 tarihle sınırlar.

Pasife alınma zamanı modelde yoktur. Bu nedenle cutoff tarihi tahmin edilmez: istenen aralıktaki bütün kayıtlı Paid/Skipped (gelecek tarihe kaydedilmiş olanlar dahil) görünür; yeni Pending yalnızca şu an aktif ve güncel takvime uygun plandan gelir. Plan adı, fiyatı, kategorisi, frequency ve **currencyCode güncel parent planından** okunur; kayıtlı Paid tutarı ActualAmount'tır. Tarihsel para birimi snapshot'ı olmadığından sonradan para birimi değiştirilmiş eski bir ödemenin orijinal para birimi geri çıkarılamaz. Bu yanıtlar böyle kayıtlar için tarihsel kur/finansal audit doğruluğu iddia etmez. Eski para birimini tahmin eden dönüşüm yoktur.

Plan ve occurrence okumaları topludur, kullanıcıya filtrelidir, AsNoTracking kullanır. Upcoming/calendar üç SQL sorgusu (timezone/planlar/occurrence'lar), dashboard aktif sayımıyla dört sorgu kullanır; N+1 yoktur. Boş ilgili plan kümesinde occurrence sorgusu atlanır ve bir sorgu azalır. Plan kimlikleriyle sınırlandırma, sorgular arasında yeni plan eklenirse eşleşmeyen kaydın projeksiyona girmesini önler. Okumalar tek point-in-time snapshot değildir; eşzamanlı durum/tarife değişikliğinde yeniden GET gerekebilir. Uçlar hiçbir occurrence veya ayar kaydı oluşturmaz. Faz 6 JWT ve aktif kullanıcı doğrulaması bu uçlarda da uygulanır.

## Faz 6 — Authentication sözleşmesi

| Uç | Başarı | Kimlik |
| --- | --- | --- |
| POST /api/auth/register | 201 UserResponse | Anonim |
| POST /api/auth/login | 200 AuthResponse | Anonim |
| POST /api/auth/refresh | 200 AuthResponse | Refresh token sahipliği |
| POST /api/auth/logout | 204 | Refresh token sahipliği |
| GET /api/users/me | 200 UserResponse | Bearer JWT |

Register body: `{"email":"user@example.com","password":"ExamplePassword123!","displayName":"Example User"}`. Email en fazla 254, DisplayName 1–100, parola 12–128 karakter. Email/ad trim edilir, parola edilmez. Register otomatik login yapmaz; UserResponse `{id,email,displayName}`. Duplicate email 409; geçersiz alanlar 400 ProblemDetails.

Login body: `{"email":"user@example.com","password":"ExamplePassword123!"}`. AuthResponse: `{accessToken,refreshToken,accessTokenExpiresAtUtc,refreshTokenExpiresAtUtc,user:{id,email,displayName},tokenType:"Bearer"}`. Zamanlar UTC ISO 8601. Yanlış email/parola/pasif/kilitli hesap aynı genel 401. Auth ve /me no-store/no-cache.

Refresh/logout body: `{"refreshToken":"<64 karakter base64url>"}`. Yanlış biçim 400; bilinmeyen/expired/revoked token refresh 401. Rotation tek kullanımlık; reuse aileyi kapatır. Logout doğru biçimli bilinmeyen veya tekrar token ile de 204. Access token logout sonrasında kısa ömrü dolana kadar geçerlidir. İstemci refresh isteklerini tekilleştirmelidir; yarışın kaybedeni kazananın child token ailesini de iptal eder.

Tüm korunan uçlara `Authorization: Bearer <accessToken>` gönderin; UserId doğrulanan sub üzerinden alınır. /me yalnızca id/email/displayName döndürür; password/hash dönmez. Auth bütçesi IP başına 20/dakika; aşım 429 ProblemDetails + Retry-After:60. Beş yanlış parola 15 dakika hesap kilidi. Güvenlik/proxy ayarları, JWT claim ve süreler, React Native güvenli token saklama sözleşmesi [SECURITY.md](SECURITY.md) içindedir. DTO örneklerindeki token yer tutucuları gerçek token değildir.

## Faz 7 — Reminder, Device ve Settings

Dokuz yeni route/işlem [NOTIFICATIONS.md](NOTIFICATIONS.md#http-sözleşmesi) içinde tanımlıdır: reminder POST/GET/PUT/DELETE; device POST/GET/DELETE; settings GET/PUT. JWT ve aktif kullanıcı zorunludur. UserId body üzerinden alınmaz; başka kullanıcı kaynağı 404, eksik JWT 401. Reminder duplicate 409; DaysBefore 0–365, saat tam HH:mm ve varsayılan 09:00. Device POST idempotent own-token upsert 200 döner, token değişimi mevcut kendi Id ile yapılır; push token yanıt/log içine konmaz. Settings timezone PUT geçersiz kimliği reddeder, legacy okuma UTC fallback döndürür.

Bildirimler HTTP isteğinde gönderilmez. Kural/ayar/cihaz değişiklikleri henüz provider tarafından kabul edilmemiş işleri iptal eder; worker uygun kaydı güncel bilgilerle tekrar zamanlayabilir. Device DELETE sonrası token silinir/tombstone olur. Auth logout push cihaz kaydını otomatik silmez; istemci önce kendi device Id için DELETE yapmalıdır. Expo ticket/receipt, retry ve teslimat sınırları aynı dokümandadır.

## Faz 8 — Subscription ve trial API

POST/GET `/api/subscriptions`, GET/PUT/DELETE `/api/subscriptions/{id}`, POST `/api/subscriptions/{id}/cancel`, GET `/api/subscriptions/trials/expiring?withinDays=7`, GET `/api/subscriptions/summary` eklendi. Tam body/response/lifecycle sözleşmesi [SUBSCRIPTIONS.md](SUBSCRIPTIONS.md) içindedir.

Create 201/Location/ETag; PUT/cancel 200/yeni ETag; DELETE güçlü If-Match ile204. PUT ve cancel body rowVersion ister. JSON enumlar string, unknown body alanı reddedilir. Geçersiz alan/geçiş400, stale version veya terminal update409, cross-user404, JWT yok401. Liste Status/ProviderName/CurrencyCode/Search ve bounded pagination kullanır. FirstBillingDate açıkça verilir ve TrialEndDate değerinden sonra olmak zorundadır; NextBillingDate/DaysRemaining timezone üzerinden hesaplanır.

Summary EstimatedAmount değerleri para birimi bazında tahmini aylık karşılıktır, gerçek tahsilat değildir. Cancel/delete linked planı pasifleştirir; Paid/Skipped geçmişini değiştirmez. Provider hesabında gerçekten iptal doğrulanmaz. Trial reminder tercihi EnableTrialReminders üzerinden yönetilir; bağımsız worker yoktur.
