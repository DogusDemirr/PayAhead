# Geliştirme

Faz 1 altyapısı, Faz 2 CRUD, Faz 3 recurrence hesaplayıcısı ve Faz 4 occurrence durum/geçmiş işlemleri uygulanmıştır. Her yeni faz öncesinde mevcut model, testler ve ilerleme kaydı incelenir; yalnızca onaylanmış faz tamamlanır, ardından gerçek doğrulamalar ve dokümantasyon güncellenip durulur. Faz 5 için açık onay gerekir.

VS Code çalışma alanını kök dizinden açın. `global.json` yalnızca .NET 8 SDK 8.0.4xx patch sürümlerine izin verir, C# 12 sabittir. Proje içindeki SDK kaynak kontrolüne alınmaz. .slnx istenen envanter dosyasıdır; .NET 8 uyumluluğu için .sln kullanılır. Yeni proje eklenirse iki dosyayı da güncelleyin.

Restore/build/test ve API başlatma komutları README içindedir. Testler xUnit kullanır. Birim testleri geliştirme kimliği güvenlik sınırını ve SQL model metaverisini doğrular. Entegrasyon testleri HTTP başlangıcı ve gerçek SQL Server migration/constraint/transaction/readiness davranışını doğrular. Model metaverisi testi gerçek veritabanı testi yerine geçmez.

SQL Server testinde SQLite veya EF InMemory kullanılmaz. Testcontainers.MsSql 4.8.1 varsayılan olarak SQL Server 2022 Linux container'ı oluşturur. PAYAHEAD_TEST_SQLSERVER verilirse harici gerçek SQL Server kullanılabilir. Her iki yolda da test rastgele adlandırılmış kendi veritabanını oluşturur ve siler; ortak geliştirme/üretim veritabanı kullanılmaz. Docker yoksa varsayılan test başarısız olur, atlanmaz. `AddInMemoryCollection` kullanımları yalnızca yapılandırma içindir; veritabanı sağlayıcısı değildir.

Her faz sonunda `docs/IMPLEMENTATION_PROGRESS.md` gerçek komut ve sonuçlarla güncellenir. Başarısız/atlanmış testler başarılı sayılmaz. SQL bağımlılığı yoksa engel açıkça kaydedilir.

## Docker geliştirme akışı

README'deki Docker bölümünü izleyin: `.env.example` → yerel `.env`, `docker compose --env-file .env -f docker/compose.yml up -d --wait`, ConnectionStrings__PayAhead ayarı, mevcut migration'ın uygulanması ve API başlatma. `stop sqlserver` verileri korur; volume silmeyin. Host 14333 portu doluysa `.env` portunu değiştirin.

`dotnet test tests/PayAhead.UnitTests --no-build` birim testlerini, `dotnet test tests/PayAhead.IntegrationTests --no-build` entegrasyonu çalıştırır. Testcontainers kendi rastgele host portunu seçer. İlk çalışmada SQL imajı ve Ryuk kaynak temizleme imajı indirilir. Docker Engine erişimi ve registry ağı gerekir; Ryuk'u devre dışı bırakmayın.

API pipeline testleri üretim merkezi hata middleware'ini 500 ve 409 senaryolarında kontrol eder; yanıtlar ProblemDetails olur, iç hata mesajı/stack trace dönmez. Gerçek API host testleri Development kimliğinin Development'ta açıkça etkinleştirilebildiğini ve Production/Staging'de başlangıcın reddedildiğini doğrular. Faz 2 ödeme endpoint testleri gerçek SQL Server'da CRUD, izolasyon, concurrency, filtre ve hata sözleşmelerini kontrol eder.

Docker CLI bulunamadığında yeni terminal açın veya README'deki kurulum dizinini oturum PATH'ine ekleyin. `docker info` başarısızsa önce Engine'i çözün. SQL `unhealthy` ise Compose loglarını inceleyin; parola, bellek, disk ve sqlcmd yolu kontrol edilir. `dotnet --list-sdks` sistem SDK'larını, `.tools/dotnet/dotnet --list-sdks` proje SDK'sını gösterir. Bu doğrulama .NET 8.0.426 ile yapılmıştır; .NET 9 ile alınmış build değildir.

## Recurrence hesaplayıcısını kullanma ve test etme

```csharp
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

IPaymentRecurrenceCalculator calculator = new PaymentRecurrenceCalculator();
var plan = new PaymentPlan
{
    StartDate = new DateOnly(2026, 1, 31),
    Frequency = PaymentFrequency.Monthly,
    Interval = 1,
    IsActive = true
};
var dates = calculator.CalculateOccurrences(plan, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
// 2026-01-31, 2026-02-28, 2026-03-31
var next = calculator.GetNextOccurrence(plan, new DateOnly(2026, 2, 28));
// 2026-02-28: referans dahil.
```

Application servisleri aynı arayüzü constructor injection ile alabilir; `AddApplication` kaydı hazırdır. Örnek yalnızca hesaplama için asgari modeldir; API üzerinden kayıt oluşturma kuralları hâlâ geçerlidir. Hesaplayıcı hiçbir tarih kaydı saklamaz ve modelin StartDate/AnchorDay alanlarını değiştirmez.

AnchorDay aylık/yıllık sonraki tekrar gününü belirler; null ise orijinal başlangıç günü korunur. StartDate her zaman ilk occurrence'dır. Örneğin 2026-01-15 ve AnchorDay=31 için ilk ödeme 15 Ocak, sonraki ödeme 28 Şubat'tır. Kısa aylarda 28/29/30'a kısaltma sonraki ayı etkilemez. Yıllık 29 Şubat planı artık yıllarda tekrar 29 Şubat'a döner; 2100 artık yıl değildir. Tam kurallar [mimari dokümanda](ARCHITECTURE.md).

Plan pasifse sonuç boştur; tarihçe görüntülemek için bu servisi kullanmayın. Geçersiz model/aralık ve 10.000 sonuç sınırını aşma DomainRuleException üretir. Aralığı daraltın; örneğin 100 yıllık günlük planı tek listeye almaya çalışmayın. Geniş aralık kendi başına yasak değildir: seyrek yıllık planda 9999 yılın tamamı sonuç sınırı içinde olabilir. GetNextOccurrence referansa matematiksel olarak atlar; bütün geleceği listelemez. DateOnly.MinValue/MaxValue hesaplayıcı tarafından desteklenir; CRUD StartDate validation kuralları gevşetilmedi.

```powershell
dotnet restore PayAhead.sln
dotnet build PayAhead.sln --no-restore
# Yalnızca saf Domain recurrence testleri; Docker gerekmez:
dotnet test tests/PayAhead.UnitTests --no-build --filter FullyQualifiedName~RecurrenceTests
# SQL DateOnly round-trip; gerçek Testcontainers SQL Server gerekir:
dotnet test tests/PayAhead.IntegrationTests --no-build --filter FullyQualifiedName~RecurrenceIntegrationTests
# Eski CRUD testleri de dahil tüm regresyon:
dotnet test PayAhead.sln --no-build
```

Recurrence birim testleri sabit girdilerle tam beklenen tarih dizilerini karşılaştırır; ay sonu/leap/interval/sınır/taşma/limit durumlarını içerir. SQL testleri migration uygulanmış izole test veritabanından gerçek EF DateOnly alanlarını tekrar okuyup aynı hesapları yapar ve PaymentOccurrence tablosuna yazılmadığını doğrular. Üretim/geliştirme verisi değiştirilmez.

Temsilî iş yükü testleri 10 yıl aylık (120), 30 yıl yıllık (30), 5 yıl günlük (1826) tarih için tam listeyi doğrular ve 100 çağrının yerel Stopwatch sürelerini TRX test çıktısına kaydeder. Bu bir mikro ölçümdür; Debug/JIT/GC/makine yüküne bağlıdır, production throughput veya BenchmarkDotNet sonucu değildir. Süreye bağlı kırılgan assertion eklenmedi. Büyük interval ve yıl 1→9999 dar sorgu testleri ayrıca matematiksel atlama/overflow davranışını doğrular.

## Occurrence durumlarını doğrulama

`dotnet test tests/PayAhead.UnitTests --no-build --filter FullyQualifiedName~OccurrenceTests` saf Domain/Application kurallarını; `dotnet test tests/PayAhead.IntegrationTests --no-build --filter FullyQualifiedName~OccurrenceApiTests` gerçek SQL Server transaction/API davranışını çalıştırır. Tüm regresyon için `dotnet test PayAhead.sln --no-build` kullanın.

SQL testleri eşzamanlı istekler, unique/FK/rowversion, SQL yazımı sonrası enjekte edilmiş hata ile rollback ve kilitli parent değişiklik/silme yarışını içerir. Kilit yarışları test-only DbCommandInterceptor sinyaliyle koordine edilir; rastgele sleep'e dayanan test yazılmadı. Aynı plandaki status yazmaları serialize edildiği için yüksek hacim performansı üretim sertleştirmesinde ayrıca ölçülmelidir. Üç yeni uç için örnekler README ve API.md içindedir.
