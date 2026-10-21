# PayAhead Backend

React Native istemcisi için .NET 8 / C# 12 / EF Core 8 / SQL Server tabanlı backend. Bu teslim yalnızca Faz 1 altyapısını kapsar. CRUD, yinelenme motoru, gerçek kullanıcı girişi ve bildirimler henüz uygulanmamıştır.

## Gereksinimler

- .NET 8 SDK 8.0.4xx ve VS Code C# desteği.
- SQL Server 2022 veya uyumlu SQL Server; isteğe bağlı Docker Compose.
- NuGet erişimi. Parolalar ortam değişkeni veya gizli anahtar deposunda tutulmalıdır.

SDK bu makinede `.tools/dotnet` içine kurulmuştur ve Git dışında tutulur. PATH üzerinde .NET 8 yoksa aşağıdaki `dotnet` yerine `.tools/dotnet/dotnet.exe` kullanın. Yerel kurulumla `$env:DOTNET_ROOT="$PWD/.tools/dotnet"` ve `$env:DOTNET_CLI_HOME="$PWD/.tools/cli"` ayarlayın.

## Yerel kurulum (PowerShell)

```powershell
dotnet restore PayAhead.sln
dotnet tool restore
dotnet build PayAhead.sln --no-restore
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__PayAhead='Server=localhost;Database=PayAheadDevelopment;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
dotnet run --project src/PayAhead.Api --no-launch-profile --urls http://localhost:5080
```

Migration komutu hedef veritabanını açıkça seçtikten sonra elle çalıştırılır. Uygulama açılışta şemayı değiştirmez. Development ayarındaki bağlantı parola içermez ve yalnızca yerel Windows kimliği içindir. Production için geçerli sertifika ile `TrustServerCertificate=False` kullanın.

- Swagger: http://localhost:5080/swagger
- Liveness: http://localhost:5080/health/live
- Readiness (bağlantı ve migration): http://localhost:5080/health/ready

```powershell
dotnet test PayAhead.sln --no-build
# Gerçek SQL Server testi; rastgele test veritabanı oluşturur ve temizler:
$env:PAYAHEAD_TEST_SQLSERVER='Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/PayAhead.IntegrationTests
dotnet list PayAhead.sln package --vulnerable --include-transitive
```

SQL Server değişkeni yoksa veritabanı testi açıkça atlanır. HTTP başlangıç testi veritabanı doğrulaması değildir.

## Yapılandırma ve sorun giderme

`ConnectionStrings__PayAhead` bağlantıyı, `ASPNETCORE_ENVIRONMENT` ortamı, `AllowedHosts` izinli alan adlarını belirler. Production bağlantısı varsayılan olarak yoktur. Swagger yalnızca Development ortamındadır.

Geliştirme kimliği varsayılan kapalıdır. Yalnızca yerel denemelerde `DevelopmentIdentity__Enabled=true` ve boş olmayan `DevelopmentIdentity__UserId=<guid>` ayarlanabilir. Kullanıcı kaydı otomatik oluşturulmaz; HTTP başlığıyla kullanıcı seçilemez. Production/Staging ortamında etkinleştirme başlangıç hatası üretir. Gerçek JWT akışı Faz 6 kapsamındadır; o zamana kadar JWT erişimi kapalıdır.

503 readiness: SQL servisi, bağlantı izinleri ve bekleyen migration kontrol edilmelidir. SDK bulunamıyorsa global.json ile uyumlu SDK kurun. `.slnx` .NET 8 CLI tarafından desteklenmediği için `.sln` derleme dosyası kullanılır; ikisi aynı altı projeyi listeler.

Ayrıntılar: [mimari](docs/ARCHITECTURE.md), [veritabanı](docs/DATABASE.md), [API](docs/API.md), [geliştirme](docs/DEVELOPMENT.md), [dağıtım](docs/DEPLOYMENT.md), [ilerleme](docs/IMPLEMENTATION_PROGRESS.md), [üretim kontrolü](docs/PRODUCTION_CHECKLIST.md).
