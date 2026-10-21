# PayAhead Backend

React Native istemcisi için .NET 8 / C# 12 / EF Core 8 / SQL Server tabanlı backend. Faz 1 altyapısı, Faz 2 ödeme planı CRUD ve Faz 3 bağımsız recurrence engine tamamlandı. Gerçek kullanıcı girişi ve bildirimler sonraki fazlardadır.

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

SQL Server değişkeni yoksa Testcontainers otomatik olarak ayrı bir SQL Server 2022 Linux container'ı başlatır. Docker erişilemiyorsa test başarısız olur; sessizce atlanmaz. HTTP başlangıç testi tek başına veritabanı doğrulaması değildir.

## Docker ile SQL Server (PowerShell)

Docker Desktop Linux container modunda çalışmalıdır. Terminal Docker'ı görmüyorsa yeni terminal açın; bu makinede yalnızca oturum için şu dizin eklenebilir:

```powershell
$env:PATH="$env:LOCALAPPDATA/Programs/DockerDesktop/resources/bin;$env:PATH"
docker --version
docker info
docker compose version
dotnet --list-sdks
```

Sistem PATH'inde yalnızca .NET 9 SDK varsa bu doğrulama için yeterli değildir: proje içindeki `.tools/dotnet/dotnet.exe --list-sdks` ile 8.0.426 doğrulanmıştır. Komutlarda bu SDK'yı kullanın.

`.env.example` şablonudur; gerçek parola içermez. `.env` Git tarafından dışlanır. Bu doğrulama sırasında `.env` içine rastgele yerel parola oluşturuldu. Yeni kurulumda `Copy-Item .env.example .env` yaptıktan sonra en az 16 karakterlik güçlü parolayı yerel dosyada belirleyin; var olan `.env` dosyasını ezmeyin. Değerleri tırnaksız ve tek satır yazın. Aşağıdaki PowerShell yükleme yöntemi için parolada noktalı virgül ve ters eğik çizgi kullanmayın.

```powershell
docker compose --env-file .env -f docker/compose.yml config --quiet
docker compose --env-file .env -f docker/compose.yml up -d --wait --wait-timeout 180
docker compose --env-file .env -f docker/compose.yml ps
$localConfig=Get-Content .env -Raw | ConvertFrom-StringData
$env:ConnectionStrings__PayAhead="Server=127.0.0.1,$($localConfig.PAYAHEAD_SQL_PORT);Database=PayAheadDockerDevelopment;User Id=sa;Password=$($localConfig.PAYAHEAD_SQL_PASSWORD);Encrypt=True;TrustServerCertificate=True"
dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/PayAhead.Api --no-build --no-launch-profile --urls http://localhost:5080
# API'yi Ctrl+C ile kapatın. Verileri koruyarak SQL container'ını durdurmak:
docker compose --env-file .env -f docker/compose.yml stop sqlserver
# Yeniden başlatmak:
docker compose --env-file .env -f docker/compose.yml up -d --wait
```

Proje adı `payahead-phase1`, container adı Compose tarafından `payahead-phase1-sqlserver-1`, named volume `payahead-phase1_sql-data` olur. Port yalnızca 127.0.0.1'e açılır; varsayılan 14333, container iç portu 1433'tür. 14333 doluysa `.env` içinde başka boş port seçin; mevcut servisleri durdurmayın. `down -v` kullanmayın: volume verilerini siler. SA hesabı yalnızca yerel geliştirme içindir.

```powershell
dotnet restore PayAhead.sln
dotnet build PayAhead.sln --no-restore
dotnet test tests/PayAhead.UnitTests --no-build
# Ayrı, geçici SQL Server container'ı ve rastgele test veritabanı:
Remove-Item Env:PAYAHEAD_TEST_SQLSERVER -ErrorAction SilentlyContinue
dotnet test tests/PayAhead.IntegrationTests --no-build
```

İsteğe bağlı `PAYAHEAD_TEST_SQLSERVER` verilirse test yalnızca o sunucuda kendi `PayAheadTest_<guid>` veritabanını oluşturur ve siler. Geliştirme veritabanına dokunmaz. Varsayılan Testcontainers dinamik port ve geçici parola kullanır; container test bitince temizlenir.

Docker sorunları: `docker info` bağlantı hatasında Docker Desktop Engine'i ve Linux modunu kontrol edin. `docker-credential-desktop` bulunamazsa Docker bin dizinini oturum PATH'ine ekleyin. `unhealthy` durumunda `docker compose --env-file .env -f docker/compose.yml logs --tail 100 sqlserver` ile parola politikası, bellek ve sqlcmd hatalarını inceleyin; yapılandırmayı gizli değerlerle ekrana döken `config` veya filtresiz `inspect` çıktısını paylaşmayın. SQL ilk açılışı zaman alabilir. `.env` parolasını değiştirmek mevcut volume içindeki SA parolasını değiştirmez. Build sırasında dosya kilidi görülürse kendi çalışan API oturumunuzu kapatın. Windows sandbox erişim hataları Docker/SQL arızası gibi raporlanmamalıdır.

## Yapılandırma ve sorun giderme

`ConnectionStrings__PayAhead` bağlantıyı, `ASPNETCORE_ENVIRONMENT` ortamı, `AllowedHosts` izinli alan adlarını belirler. Production bağlantısı varsayılan olarak yoktur. Swagger yalnızca Development ortamındadır.

Geliştirme kimliği varsayılan kapalıdır. Yalnızca yerel denemelerde `DevelopmentIdentity__Enabled=true` ve boş olmayan `DevelopmentIdentity__UserId=<guid>` ayarlanabilir. Kullanıcı kaydı otomatik oluşturulmaz; HTTP başlığıyla kullanıcı seçilemez. Production/Staging ortamında etkinleştirme başlangıç hatası üretir. Gerçek JWT akışı Faz 6 kapsamındadır; o zamana kadar JWT erişimi kapalıdır.

503 readiness: SQL servisi, bağlantı izinleri ve bekleyen migration kontrol edilmelidir. SDK bulunamıyorsa global.json ile uyumlu SDK kurun. `.slnx` .NET 8 CLI tarafından desteklenmediği için `.sln` derleme dosyası kullanılır; ikisi aynı altı projeyi listeler.

Ayrıntılar: [mimari](docs/ARCHITECTURE.md), [veritabanı](docs/DATABASE.md), [API](docs/API.md), [geliştirme](docs/DEVELOPMENT.md), [dağıtım](docs/DEPLOYMENT.md), [ilerleme](docs/IMPLEMENTATION_PROGRESS.md), [üretim kontrolü](docs/PRODUCTION_CHECKLIST.md).

## Faz 2 — Ödeme planlarını Development ortamında deneme

Beş uç: POST/GET `/api/payments`, GET/PUT/DELETE `/api/payments/{id}`. Tam örnekler, filtreler ve rowversion sözleşmesi [API dokümanında](docs/API.md). PUT body içindeki rowVersion zorunludur; DELETE son ETag'i If-Match başlığında ister. İlişkili geçmiş/hatırlatma varsa silme 409 döner. Yeni migration gerekmedi; Name için 150 karakter uygulama sınırı, mevcut nvarchar(200) kolonunu daraltmadan uygulanır.

Gerçek kayıt/giriş henüz olmadığından test için yalnızca Development kimliği kullanılır. SQL Server'a bağlı SSMS veya VS Code SQL oturumunda **yalnızca geliştirme veritabanını seçerek** aşağıdaki test kullanıcısını ekleyin. Var olan kullanıcı kaydı değiştirilmez; bu kayıtla parola girişi yapılamaz (PasswordHash, giriş özelliği olmayan geliştirme kimliği için kullanılamaz bir işarettir).

```sql
USE PayAheadDockerDevelopment;
DECLARE @UserId uniqueidentifier = '33333333-3333-4333-8333-333333333333';
IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId)
INSERT INTO Users (Id, Email, NormalizedEmail, DisplayName, PasswordHash, IsActive, CreatedAtUtc, UpdatedAtUtc)
VALUES (@UserId, 'dev-payments@example.invalid', 'DEV-PAYMENTS@EXAMPLE.INVALID', 'Development user',
        'development-identity-only', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
```

Docker bağlantısını yukarıdaki gibi ortam değişkenine yükledikten sonra:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:DevelopmentIdentity__Enabled='true'
$env:DevelopmentIdentity__UserId='33333333-3333-4333-8333-333333333333'
dotnet run --project src/PayAhead.Api --no-build --no-launch-profile --urls http://localhost:5080
```

Swagger http://localhost:5080/swagger üzerinden POST, list, get ve put deneyin. POST response Location ve ETag döndürür. Başka terminalde örnek istek:

```powershell
$payload=@{name='İnternet';amount=499.90;currencyCode='TRY';category='Bills';frequency='Monthly';interval=1;startDate='2026-10-31'}
$created=Invoke-WebRequest -UseBasicParsing -Method Post http://localhost:5080/api/payments -ContentType 'application/json; charset=utf-8' -Body ($payload | ConvertTo-Json)
$payment=$created.Content | ConvertFrom-Json
Invoke-RestMethod "http://localhost:5080/api/payments?currencyCode=TRY&pageNumber=1&pageSize=20"
Invoke-RestMethod "http://localhost:5080/api/payments/$($payment.id)"
$payload.amount=599.90
$payload.rowVersion=$payment.rowVersion
$updated=Invoke-WebRequest -UseBasicParsing -Method Put "http://localhost:5080/api/payments/$($payment.id)" -ContentType 'application/json; charset=utf-8' -Body ($payload | ConvertTo-Json)
Invoke-WebRequest -UseBasicParsing -Method Delete "http://localhost:5080/api/payments/$($payment.id)" -Headers @{'If-Match'=$updated.Headers['ETag']}
```

UserId'yi body veya başlıkla göndermeyin; sunucunun sabit Development yapılandırması kullanılır. Başka kullanıcı izolasyonunu denemek için ayrı sunucu oturumunda farklı, mevcut test UserId'si seçilmelidir. Üretim/Staging ortamında bu mekanizma başlangıçta reddedilir. İş bitince API'yi kapatıp `DevelopmentIdentity__Enabled` ve `DevelopmentIdentity__UserId` ortam değişkenlerini kaldırın.

`dotnet test PayAhead.sln` mevcut CRUD ve yeni recurrence testlerini birlikte çalıştırır. SQL testleri Testcontainers üzerinde izole veritabanı kullanır; geliştirme verilerini silmez. Recurrence yalnızca Domain hesabıdır, yeni HTTP ucu veya geleceğe occurrence kaydı eklemez. Anchor ve 10.000 sonuç sınırı için [mimari](docs/ARCHITECTURE.md) ve [geliştirme](docs/DEVELOPMENT.md) dokümanlarına bakın. Faz 4 için ayrı onay gerekir.
