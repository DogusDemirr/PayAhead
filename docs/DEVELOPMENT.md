# Geliştirme

Plan: (1) ortam/depo incelemesi; (2) altı proje ve mimari referanslar; (3) model/SQL konfigürasyonu/migration; (4) API/DI/log/hata/sağlık/geliştirme kimliği; (5) gerçek doğrulamalar ve dokümantasyon; (6) Faz 2 onayını bekleme.

VS Code çalışma alanını kök dizinden açın. `global.json` yalnızca .NET 8 SDK 8.0.4xx patch sürümlerine izin verir, C# 12 sabittir. Proje içindeki SDK kaynak kontrolüne alınmaz. .slnx istenen envanter dosyasıdır; .NET 8 uyumluluğu için .sln kullanılır. Yeni proje eklenirse iki dosyayı da güncelleyin.

Restore/build/test ve API başlatma komutları README içindedir. Testler xUnit kullanır. Birim testleri geliştirme kimliği güvenlik sınırını ve SQL model metaverisini doğrular. Entegrasyon testleri HTTP başlangıcı ve gerçek SQL Server migration/constraint/transaction/readiness davranışını doğrular. Model metaverisi testi gerçek veritabanı testi yerine geçmez.

SQL Server testinde SQLite veya EF InMemory kullanılmaz. Testcontainers.MsSql 4.8.1 varsayılan olarak SQL Server 2022 Linux container'ı oluşturur. PAYAHEAD_TEST_SQLSERVER verilirse harici gerçek SQL Server kullanılabilir. Her iki yolda da test rastgele adlandırılmış kendi veritabanını oluşturur ve siler; ortak geliştirme/üretim veritabanı kullanılmaz. Docker yoksa varsayılan test başarısız olur, atlanmaz. `AddInMemoryCollection` kullanımları yalnızca yapılandırma içindir; veritabanı sağlayıcısı değildir.

Her faz sonunda `docs/IMPLEMENTATION_PROGRESS.md` gerçek komut ve sonuçlarla güncellenir. Başarısız/atlanmış testler başarılı sayılmaz. SQL bağımlılığı yoksa engel açıkça kaydedilir.

## Docker geliştirme akışı

README'deki Docker bölümünü izleyin: `.env.example` → yerel `.env`, `docker compose --env-file .env -f docker/compose.yml up -d --wait`, ConnectionStrings__PayAhead ayarı, mevcut migration'ın uygulanması ve API başlatma. `stop sqlserver` verileri korur; volume silmeyin. Host 14333 portu doluysa `.env` portunu değiştirin.

`dotnet test tests/PayAhead.UnitTests --no-build` birim testlerini, `dotnet test tests/PayAhead.IntegrationTests --no-build` entegrasyonu çalıştırır. Testcontainers kendi rastgele host portunu seçer. İlk çalışmada SQL imajı ve Ryuk kaynak temizleme imajı indirilir. Docker Engine erişimi ve registry ağı gerekir; Ryuk'u devre dışı bırakmayın.

API pipeline testleri üretim merkezi hata middleware'ini 500 ve 409 senaryolarında kontrol eder; yanıtlar ProblemDetails olur, iç hata mesajı/stack trace dönmez. Gerçek API host testleri Development kimliğinin Development'ta açıkça etkinleştirilebildiğini ve Production/Staging'de başlangıcın reddedildiğini doğrular. İş endpoint'i eklenmedi.

Docker CLI bulunamadığında yeni terminal açın veya README'deki kurulum dizinini oturum PATH'ine ekleyin. `docker info` başarısızsa önce Engine'i çözün. SQL `unhealthy` ise Compose loglarını inceleyin; parola, bellek, disk ve sqlcmd yolu kontrol edilir. `dotnet --list-sdks` sistem SDK'larını, `.tools/dotnet/dotnet --list-sdks` proje SDK'sını gösterir. Bu doğrulama .NET 8.0.426 ile yapılmıştır; .NET 9 ile alınmış build değildir.
