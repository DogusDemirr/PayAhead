# Mimari

Domain on temel varlığı, enumları ve bağımsız recurrence hesaplayıcısını içerir; dış bağımlılığı yoktur. Application doğrulama kaydı, ICurrentUser sözleşmesi ve ödeme kullanım senaryolarını içerir. Infrastructure SQL Server DbContext, Fluent API yapılandırmaları ve migration içerir. API HTTP, kimlik doğrulama sınırı, DI, ProblemDetails, sağlık denetimi ve Serilog yapılandırmasını içerir.

Faz 2'de PaymentService, ödeme DTO'ları ve FluentValidation kuralları Application içinde eklendi. Ödemeye özel IPaymentPlanStore arayüzünü Infrastructure/PaymentPlanStore EF Core ile uygular. PaymentsController yalnızca HTTP eşlemesi yapar; DbContext sorgusu veya iş kuralı içermez. Domain PaymentPlan.Create/UpdateDetails temel kuralları korur. Genel repository/CQRS katmanı eklenmedi; Domain nesneleri HTTP yanıtı olarak dışarı açılmaz.

Kimliği doğrulanmamış istekler için varsayılan yetkilendirme politikası erişimi kapatır. Sağlık uçları açıkça anonimdir. Development kimliği yapılandırma ile sabitlenir; üretim ortamında etkinleştirildiğinde başlangıç reddedilir. JWT paketinin bulunması gerçek giriş akışının tamamlandığı anlamına gelmez.

Serilog istek gövdesini, query string'i veya tokenları loglamaz. EF hassas veri loglaması kapalıdır. Merkezi hata yanıtı stack trace veya veritabanı içeriği döndürmez. Readiness dışarıya bağlantı ayrıntısı vermez.

## Faz 3 — Recurrence Engine

`Domain/Recurrence/IPaymentRecurrenceCalculator` iki saf işlem tanımlar: `CalculateOccurrences(PaymentPlan, fromDate, toDate)` ve `GetNextOccurrence(PaymentPlan, referenceDate)`. `PaymentRecurrenceCalculator` bu sözleşmeyi uygular. Application DI kaydı durumsuz hesaplayıcıyı singleton olarak sunar. Saat, SQL Server, EF Core, HTTP, Controller veya dış servis bağımlılığı yoktur. Girdinin takvim alanları çağrı başında değer olarak alınır; model değiştirilmez. Aynı girdiler aynı tarihleri üretir.

Takvim Gregorian, tarihler DateOnly'dır. Aralık ve referans sınırları dahildir. `StartDate` her sıklıkta ilk ödeme tarihidir. Once yalnızca bu tarihi, Daily her Interval günü, Weekly her 7×Interval günü üretir. Monthly her Interval ayı; Yearly başlangıç ayını koruyarak her Interval yılı kullanır. Aylık/yıllık n>0 tekrarlar için gün `AnchorDay ?? StartDate.Day` olur. Bu gün hedef ayda yoksa ayın son geçerli gününe kısaltılır. Kısaltılmış tarih yeni anchor olmaz. Diğer sıklıklarda geçerli AnchorDay hesaplamayı değiştirmez.

| Başlangıç ve sıklık | Örnek sonuç |
| --- | --- |
| 2026-01-31, Monthly, anchor null | 01-31 → 02-28 → 03-31 → 04-30 → 05-31 |
| 2026-01-30, Monthly, anchor null | 01-30 → 02-28 → 03-30 → 04-30 |
| 2024-01-31, Monthly | 01-31 → 02-29 → 03-31 |
| 2024-02-29, Yearly | 2024-02-29 → 2025-02-28 → 2026-02-28 → 2027-02-28 → 2028-02-29 |
| 2026-01-15, Monthly, AnchorDay=31 | 01-15 → 02-28 → 03-31; Ocak ayında ikinci ödeme yok |
| 2026-04-30, Monthly, anchor null / 31 | Sonraki tarih sırasıyla 05-30 / 05-31; ay sonu varsayımı otomatik yapılmaz |

StartDate öncesi ve EndDate sonrası üretilmez. IsActive=false ise tarih listesi boş, next null olur; bu bir tarihçe hesaplayıcısı değildir. GetNextOccurrence referansın kendisini dahil eder ve gelecekte kayıt yoksa null döner; sistem saatini okumaz. Hesaplayıcı bütün geçerli DateOnly değerlerini destekler; mevcut CRUD'ın default StartDate reddi değişmemiştir.

Geçersiz interval/enum/anchor, EndDate<StartDate veya fromDate>toDate `DomainRuleException` üretir; null plan `ArgumentNullException` üretir. Yapısal doğrulama pasif planda da uygulanır. Liste başına en fazla **10.000** tarih döner; 10.001'inci sonuçta açık DomainRuleException oluşur, kısmi liste döndürülmez. Caller daha dar, gerekirse birbirini örtmeyen aralıklar kullanmalıdır. GetNextOccurrence liste üretmez ve bu sınıra takılmaz.

İlk tekrar indeksi gün farkı, ay indeksi veya yıl farkından hesaplanır; en fazla bir tarih düzeltmesi gerekir. Böylece çalışma maliyeti geçmiş tekrar sayısına değil döndürülen sonuç sayısına bağlıdır: liste O(min(sonuç, 10.001)), next O(1), liste belleği O(10.000) ile sınırlıdır (List kapasitesi büyüme payı içerir). Çarpımlar long ile ve DateOnly sınırına kalan mesafe kontrol edilerek yapılır; temsil edilemeyen gelecek tarih null ile sonlanır. İstisnayı normal döngü sonlandırma aracı olarak kullanmaz.

Faz 3 veritabanına occurrence yazmaz, CRUD sözleşmesini değiştirmez ve yeni HTTP ucu eklemez. PaymentOccurrence durum işlemleri Faz 4 kapsamındadır.
