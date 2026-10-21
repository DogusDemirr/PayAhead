# PayAhead Backend

React Native istemcisi için .NET8/C#12/EF8/SQL Server backend. Faz1–10 ödeme, recurrence/geçmiş/overview, JWT, notification, harici abonelik/trial, Premium ve production hardening uygulanmıştır. Gerçek Apple/Google satın alması, Expo cihaz teslimatı ve production kurulumu ayrıca doğrulanmalıdır.

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

Faz 6 ile korunan uçlar gerçek JWT ister. DevelopmentIdentity API içinde kimlik sağlamaz; test handler yalnızca test projesindedir. Production/Staging ortamında eski bayrak açık ise başlangıç reddedilir. JWT ve giriş adımları docs/DEVELOPMENT.md içindedir.

503 readiness: SQL servisi, bağlantı izinleri ve bekleyen migration kontrol edilmelidir. SDK bulunamıyorsa global.json ile uyumlu SDK kurun. `.slnx` .NET 8 CLI tarafından desteklenmediği için `.sln` derleme dosyası kullanılır; ikisi aynı altı projeyi listeler.

Ayrıntılar: [mimari](docs/ARCHITECTURE.md), [veritabanı](docs/DATABASE.md), [API](docs/API.md), [geliştirme](docs/DEVELOPMENT.md), [dağıtım](docs/DEPLOYMENT.md), [ilerleme](docs/IMPLEMENTATION_PROGRESS.md), [üretim kontrolü](docs/PRODUCTION_CHECKLIST.md).

## Faz 2 — Ödeme planlarını Development ortamında deneme

Beş uç: POST/GET `/api/payments`, GET/PUT/DELETE `/api/payments/{id}`. Tam örnekler, filtreler ve rowversion sözleşmesi [API dokümanında](docs/API.md). PUT body içindeki rowVersion zorunludur; DELETE son ETag'i If-Match başlığında ister. İlişkili geçmiş/hatırlatma varsa silme 409 döner. Yeni migration gerekmedi; Name için 150 karakter uygulama sınırı, mevcut nvarchar(200) kolonunu daraltmadan uygulanır.

Önce POST /api/auth/register ve POST /api/auth/login çağırın. Dönen access token değerini Authorization: Bearer başlığında gönderin. UserId doğrulanmış JWT sub claim üzerinden alınır. Çalıştırılabilir PowerShell akışı [DEVELOPMENT.md](docs/DEVELOPMENT.md), tam sözleşme [API.md](docs/API.md) içindedir. SQL testleri izole Testcontainers veritabanlarında çalışır.

## Faz 4 — Ödeme durumu ve kayıtlı geçmiş

Yeni uçlar: PUT `/api/payments/{paymentId}/occurrences/{dueDate}/status`, GET `/api/payments/{paymentId}/occurrences`, GET `/api/payments/{paymentId}/occurrences/{dueDate}`. Login ile aldığınız JWT değerini Authorization başlığında gönderip Swagger veya PowerShell üzerinden deneyebilirsiniz. DueDate gerçekten plan takvimine ait olmalıdır; örneğin 31 Ocak aylık planda 28 Şubat geçerlidir.

```powershell
# paymentId yerine sahip olduğunuz planın Id'sini kullanın; tarih plan takvimine uygun olmalı.
$root="http://localhost:5080/api/payments/$paymentId/occurrences"
Invoke-RestMethod -Headers $headers "$root/2026-02-28"  # Kayıt yoksa sanal Pending; GET yazma yapmaz.
$paid=Invoke-RestMethod -Headers $headers -Method Put "$root/2026-02-28/status" -ContentType 'application/json; charset=utf-8' -Body (@{status='Paid';actualAmount=299.99} | ConvertTo-Json)
$skipped=Invoke-RestMethod -Headers $headers -Method Put "$root/2026-02-28/status" -ContentType 'application/json; charset=utf-8' -Body (@{status='Skipped';rowVersion=$paid.rowVersion} | ConvertTo-Json)
Invoke-RestMethod -Headers $headers "$root`?fromDate=2026-01-01&toDate=2026-12-31&status=Skipped&pageNumber=1&pageSize=20"
Invoke-RestMethod -Headers $headers -Method Put "$root/2026-02-28/status" -ContentType 'application/json; charset=utf-8' -Body (@{status='Pending';rowVersion=$skipped.rowVersion} | ConvertTo-Json)
```

Paid tutarı zorunludur; Pending/Skipped tutar kabul etmez. Farklı durum/tutar için son **occurrence rowVersion** gerekir; plan token'ı kullanılmaz. Aynı Paid/Skipped isteği PaidAtUtc veya rowversion'ı değiştirmez. Reset Pending kaydı siler; bu geçmiş append-only audit değildir. Pasif plan geçmişi okunabilir ama değiştirilemez (409). Geçmiş listesi sadece kaydedilmiş durumları döndürür; hesaplanan takvim Faz 5 kapsamındadır. Ayrıntılı yanıt ve concurrency örnekleri [API dokümanında](docs/API.md).

## Faz 5 — Ana ekran ve takvim

Login sonrası $headers=@{Authorization="Bearer $($session.accessToken)"} hazırlayıp API çalışırken:

```powershell
Invoke-RestMethod -Headers $headers 'http://localhost:5080/api/payments/upcoming'
Invoke-RestMethod -Headers $headers 'http://localhost:5080/api/dashboard/summary'
Invoke-RestMethod -Headers $headers 'http://localhost:5080/api/calendar?year=2026&month=10'
# Tam 7 günlük ve 30 günlük pencereler:
Invoke-RestMethod -Headers $headers 'http://localhost:5080/api/dashboard/summary?fromDate=2026-10-21&toDate=2026-10-27'
Invoke-RestMethod -Headers $headers 'http://localhost:5080/api/dashboard/summary?fromDate=2026-10-21&toDate=2026-11-19'
```

Varsayılan başlangıç kullanıcı timezone'undaki bugündür; UserSettings yok/geçersizse UTC fallback, context.usedTimeZoneFallback ile görünür. Varsayılan bitiş from+30 (sınırlar dahil 31 tarih). Upcoming varsayılan Pending ve sayfalıdır; Calendar status belirtilmezse tüm durumları gösterir. Pending plan tutarı, Paid gerçek tutar kullanır; Skipped/Paid yaklaşan borca girmez. TRY/USD/EUR hiçbir zaman tek toplamda birleştirilmez. Calendar günlük toplamı Pending borcudur.

Pasif planlar yeni Pending üretmez; aralıktaki kayıtlı Paid/Skipped geçmişi korunur. Deactivation zamanı ve currency snapshot bulunmadığından eski aktiflik/para birimi tahmin edilmez; metadata güncel parent planından gelir. En fazla 366 dahil gün, 1000 ilgili plan ve 50.000 sonuç; aşımda 400 ProblemDetails, sessiz kısmi toplam yoktur. Uçlar read-only'dır ve occurrence kaydetmez. Tam DTO/örnekler [API.md](docs/API.md), test ayrıntıları [IMPLEMENTATION_PROGRESS.md](docs/IMPLEMENTATION_PROGRESS.md) içindedir.

## Faz 6 — Authentication

POST `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`; GET `/api/users/me` eklendi. Parola hashleme, JWT, rotasyon, reuse, logout ve üretim sınırları [SECURITY.md](docs/SECURITY.md) içinde; başlatma ve PowerShell akışı [DEVELOPMENT.md](docs/DEVELOPMENT.md) içindedir. .NET 8 / EF Core 8 korunmuştur.

## Faz 7 — Hatırlatmalar ve bildirim kuyruğu

Reminder CRUD, device upsert/list/delete ve kullanıcı settings GET/PUT eklendi. [NOTIFICATIONS.md](docs/NOTIFICATIONS.md) API örneklerini, timezone/DST politikasını, SQL lease/retry/receipt akışını ve üretim konfigürasyonunu açıklar. Worker varsayılan kapalıdır; gerçek Expo credential olmadan push başarı iddiası yoktur. Sağlayıcı ticket kabulü cihaz teslimatı değildir.

Faz 6–9 migration'ları mevcut Docker geliştirme veritabanına geçmişi koruyarak uygulandı; kurulumda yukarıdaki açık EF update komutunu kullanın. [İlerleme kaydı](docs/IMPLEMENTATION_PROGRESS.md) gerçek sonuçları içerir.

## Faz 8 — Harici abonelikler ve ücretsiz denemeler

Subscription CRUD/cancel, expiring trials ve summary eklendi. Harici abonelik modeli TrackedSubscription, Premium UserSubscription modelinden ayrıdır. Tek finansal kaynak linked PaymentPlan; dashboard/calendar toplamlarına ikinci kaynak eklenmez. Trial bitiş hatırlatmaları aynı SQL worker içinde ayrı hedef kullanır. Lifecycle, örnek body, rowversion, tahmini aylık karşılık ve sınırlamalar [SUBSCRIPTIONS.md](docs/SUBSCRIPTIONS.md) içinde.

## Faz 9 — Premium ve mağaza doğrulaması

Free: 5 aktif plan, plan başına 1 aktif reminder, 3 aktif harici abonelik. Premium varsayılan teknik limitleri 1000/10/1000; merkezi policy ile yapılandırılır. Süre dolması mevcut veriyi silmez; yeni oluşturma/aktifleştirme güncel limite tabidir. [PREMIUM.md](docs/PREMIUM.md) lifecycle, API ve React Native sözleşmesini; [STORE_INTEGRATION.md](docs/STORE_INTEGRATION.md) Apple JWS/Server API, Google subscriptionsv2, webhook, encryption keyring ve production kurulumunu açıklar.

Mağazalar ve reconcile worker varsayılan kapalıdır; gerçek credential olmadan Premium verilmez. Test sağlayıcısı yalnızca test assembly'lerindedir. Faz9 baseline364unit+228SQL geçti; güncel Faz10 sonuçları [ilerleme raporunda](docs/IMPLEMENTATION_PROGRESS.md). Canlı mağaza doğrulaması yapılmadı.

## Faz 10 — Production hardening

SQL-backed distributed rate limiting, bounded refresh retention, CORS/headers/correlation/metrics/worker health, güvenli JSON export ve parola recheck ile hesap silme eklendi. Docker non-root/read-only startup ve yerel backup/restore denendi. GitHub Actions PR pipeline ve manuel release-review örneği hazır; GitHub üzerinde çalıştırılmadı ve production'a deploy edilmedi. [DEPLOYMENT](docs/DEPLOYMENT.md), [OPERATIONS_RUNBOOK](docs/OPERATIONS_RUNBOOK.md), [DATA_PRIVACY](docs/DATA_PRIVACY.md) ve Completed/Pending/Blocked [checklist](docs/PRODUCTION_CHECKLIST.md) kalan release kapılarını açıklar. Frontend test API sözleşmesiyle başlayabilir; canlı ücretli entegrasyonların credential/mağaza doğrulaması tamamlanmış sayılmaz.
