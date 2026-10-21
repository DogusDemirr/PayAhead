# Mimari

Domain on temel varlığı ve enumları içerir; dış bağımlılığı yoktur. Application doğrulama kaydı ve ICurrentUser sözleşmesini içerir; kullanım senaryoları ilgili fazlarda eklenecektir. Infrastructure SQL Server DbContext, Fluent API yapılandırmaları ve migration içerir. API HTTP, kimlik doğrulama sınırı, DI, ProblemDetails, sağlık denetimi ve Serilog yapılandırmasını içerir.

Faz 2'de PaymentService, ödeme DTO'ları ve FluentValidation kuralları Application içinde eklendi. Ödemeye özel IPaymentPlanStore arayüzünü Infrastructure/PaymentPlanStore EF Core ile uygular. PaymentsController yalnızca HTTP eşlemesi yapar; DbContext sorgusu veya iş kuralı içermez. Domain PaymentPlan.Create/UpdateDetails temel kuralları korur. Genel repository/CQRS katmanı eklenmedi; Domain nesneleri HTTP yanıtı olarak dışarı açılmaz.

Kimliği doğrulanmamış istekler için varsayılan yetkilendirme politikası erişimi kapatır. Sağlık uçları açıkça anonimdir. Development kimliği yapılandırma ile sabitlenir; üretim ortamında etkinleştirildiğinde başlangıç reddedilir. JWT paketinin bulunması gerçek giriş akışının tamamlandığı anlamına gelmez.

Serilog istek gövdesini, query string'i veya tokenları loglamaz. EF hassas veri loglaması kapalıdır. Merkezi hata yanıtı stack trace veya veritabanı içeriği döndürmez. Readiness dışarıya bağlantı ayrıntısı vermez.
