# Mimari

Domain on temel varlığı ve enumları içerir; dış bağımlılığı yoktur. Application doğrulama kaydı ve ICurrentUser sözleşmesini içerir; kullanım senaryoları ilgili fazlarda eklenecektir. Infrastructure SQL Server DbContext, Fluent API yapılandırmaları ve migration içerir. API HTTP, kimlik doğrulama sınırı, DI, ProblemDetails, sağlık denetimi ve Serilog yapılandırmasını içerir.

Faz 1'de iş API'si yoktur. Application üzerinde gereksiz repository/CQRS katmanı kurulmadı. Domain nesneleri HTTP yanıtı olarak dışarı açılmaz. Sonraki fazlarda DTO ve FluentValidation kuralları Application içinde tanımlanmalıdır.

Kimliği doğrulanmamış istekler için varsayılan yetkilendirme politikası erişimi kapatır. Sağlık uçları açıkça anonimdir. Development kimliği yapılandırma ile sabitlenir; üretim ortamında etkinleştirildiğinde başlangıç reddedilir. JWT paketinin bulunması gerçek giriş akışının tamamlandığı anlamına gelmez.

Serilog istek gövdesini, query string'i veya tokenları loglamaz. EF hassas veri loglaması kapalıdır. Merkezi hata yanıtı stack trace veya veritabanı içeriği döndürmez. Readiness dışarıya bağlantı ayrıntısı vermez.
