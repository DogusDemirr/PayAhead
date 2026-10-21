# PayAhead çalışma kuralları

- Açıklamalar, ilerleme raporları ve dokümantasyon Türkçe; kod ve API adları İngilizce olmalıdır.
- Yalnızca onaylanmış fazı uygulayın. Faz 5 onaylandı; Faz 5 tamamlandıktan sonra Faz 6 için açık onay bekleyin.
- Tüm projeler net8.0, C# 12 ve EF Core 8.x kullanır. Sürüm yükseltmesi açık onay gerektirir.
- Domain bağımsızdır; Application → Domain; Infrastructure → Application/Domain; API → Application/Infrastructure.
- İş kuralları endpoint/controller içinde tutulmaz. MediatR, genel repository veya karmaşık CQRS eklemeyin.
- Tarihleri DateOnly, zaman damgalarını UTC, parayı decimal(19,4) olarak saklayın. Farklı para birimlerini toplamayın.
- Her kullanıcı kaynağını doğrulanmış UserId ile sınırlandırın. İstemcinin gönderdiği sahiplik bilgisine güvenmeyin.
- SQL Server zorunludur. SQLite/InMemory testleri SQL Server kanıtı sayılmaz.
- Kaynak kontrolüne gizli anahtar/parola eklemeyin. Hassas veri veya token loglamayın.
- DevelopmentIdentity yalnızca Development ortamında açıkça etkinleştirilebilir; diğer ortamlarda uygulama başlamamalıdır.
- Otomatik migration veya yıkıcı veritabanı işlemi eklemeyin.
- Her faz sonunda restore/build/test çalıştırın; gerçek sonuçları ve engelleri docs/IMPLEMENTATION_PROGRESS.md içine kaydedin.

## Komutlar

`dotnet restore PayAhead.sln`, `dotnet build PayAhead.sln --no-restore`, `dotnet test PayAhead.sln --no-build`.

Bu makinede SDK proje içindedir: `.tools/dotnet/dotnet.exe`. PowerShell için `$env:DOTNET_CLI_HOME="$PWD/.tools/cli"` kullanın.
SQL Server entegrasyonu varsayılan olarak Testcontainers ile ayrı SQL Server 2022 container'ı başlatır; Docker gerekir. `PAYAHEAD_TEST_SQLSERVER` isteğe bağlı harici gerçek SQL bağlantısıdır. Test rastgele `PayAheadTest_<guid>` veritabanı oluşturur ve yalnızca onu temizler. Harici test hesabına bu veritabanları için oluşturma/silme izni gerekir. Compose geliştirme altyapısını `docker compose --env-file .env -f docker/compose.yml up -d --wait` ile başlatın; `stop sqlserver` veriyi korur. Volume silmeyin.
