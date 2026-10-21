# API sözleşmesi — Faz 2

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

API model binding ve FluentValidation hataları application/problem+json döner. İç hata mesajları, SQL veya stack trace döndürülmez. Kullanıcı eşleştirmesi ve sorgu filtreleri sunucudadır. Faz 6 gelene kadar gerçek JWT kabulü kapalıdır; üretimde geliştirme kimliği etkinleştirilirse uygulama başlamaz. Development'ta test kullanıcısı hazırlama ve başlatma adımları README'dedir. Register/login/refresh ve takvim/özet uçları henüz yoktur. Faz 3 recurrence engine yalnızca Domain servisi olarak kullanılabilir; bu API sözleşmesine yeni uç veya otomatik occurrence üretimi eklemez.
