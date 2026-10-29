> **Phase 5.3.2 LAN düzeltmesi:** Bu makinenin yeni LAN IP’si192.168.1.110; önceki192.168.1.155 adresi ve Development AllowedHosts kaydı erişim/HTTP400 hatası üretiyordu. Mobil .env ile açık Development host listesi hizalandı. Production listesi ve host güvenliği korunur. [Fotoğraf tanısı](PHASE5_3_2_DIAGNOSIS.md).

> **Phase 5.3.1:** Fotoğraf için [özel yerel storage ve Avatars__Root](AVATARS.md) yönergesini izleyin. Eski API süreci güncel kod sayılmaz; `scripts/Start-Development.ps1` güncel kaynakları derler. Development ProfileAvatar migration durumu doğrulanmalıdır.

## Phase 5.3 — Güncel backend gerekir

ProfileSecurity migration'ı doğrulanmış Docker Development DB'ye uygulandı. Android öncesi eski API terminalini kontrollü kapatıp `scripts/Start-Development.ps1 -Port 5080` ile yeni kodu başlatın; port/LAN değeri mobile/.env ile eşleşsin. Yeni GET/PUT me/profile ve POST me/change-password endpoint'leri eski binary'de yoktur. [Kurulum, session politikası, test ve silme saklama ayrıntıları](ACCOUNT_MANAGEMENT.md). Otomatik migration, production deployment veya gerçek push teslimi bu fazda yoktur.

## Phase5.1.1 — Güncel katalog için yeniden başlatma

Bu makinenin doğrulanmış Development DB'sine `20261025145738_CatalogCoverage` uygulandı;pending0. Başka ortamda önce hedef/pendingmigration kontrolü ve yedek/inceleme yapın; startup migrate yoktur. Sonra açık eski API terminalini Ctrl+C ile kontrollü kapatıp `./scripts/Start-Development.ps1 -Port 5080` çalıştırın. Güncel kod ayrı artifacts çıktısında derlenir; eski `src/bin` DLL'i `--no-build` ile çalıştırmayın. Bu oturum mevcut eski API'yi durdurmadı; güncel API ayrı5086portunda doğrulandı.

Katalog testleri: `.tools/dotnet/dotnet.exe test PayAhead.sln --filter FullyQualifiedName~Catalog`;tam restore/build/test de çalıştırılmalıdır. Açık süreç default output DLL'lerini kilitliyorsa `--artifacts-path artifacts/phase511-check` ile restore/build/test aynı yolu kullanın. Docker Engine SQL Testcontainers için gereklidir. [Rapor](../mobile/docs/PHASE5_1_1_REPORT.md), [model/API](CATALOG.md), [kategori denetimi](CATALOG_AUDIT.md).

## Phase5.1 — Kataloğu çalıştırma ve doğrulama

Hedef development DB ve pending migration listesini önce doğrulayın. Secret connection string'i komut/loga yazmayın. SQL Compose'u mevcut `.env` ile `docker compose --env-file .env -f docker/compose.yml up -d --wait` kullanarak başlatın; volume silmeyin. `stop sqlserver` veriyi korur.

```powershell
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
$env:ASPNETCORE_ENVIRONMENT='Development'
& ./.tools/dotnet/dotnet.exe tool restore
& ./.tools/dotnet/dotnet.exe ef migrations list --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
# Yalnız doğru hedef ve bekleyen migration doğrulandıktan sonra:
& ./.tools/dotnet/dotnet.exe ef database update 20261025145738_CatalogCoverage --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
& ./.tools/dotnet/dotnet.exe restore PayAhead.sln
& ./.tools/dotnet/dotnet.exe build PayAhead.sln --no-restore
& ./.tools/dotnet/dotnet.exe test PayAhead.sln --no-build
```

Testcontainers gerçek SQL2022 için Docker Engine gerektirir. `--filter FullyQualifiedName~Catalog` katalog testlerini seçer; tam regresyonu ayrıca çalıştırın. SQL yokken başarı raporlamayın. Startup migration yapmaz. Bu makinenin Development DB'sine CatalogFoundation zaten uygulandı (pending0). Eski referanslar otomatik doldurulmadı. [Şema/API/cache/seed](CATALOG.md), [19 maddeli sonuç](../mobile/docs/PHASE5_1_REPORT.md).

# Geliştirme

Frontend Phase 4 ayrıca onaylanarak uygulanmıştır: [şifre sıfırlama, SMTP/Mailpit, development migration ve Expo bağlantısı](PASSWORD_RESET.md), [analiz API](ANALYTICS.md). E-posta varsayılan kapalı; provider kimlik bilgileri yalnızca environment/User Secrets. Gerçek inbox ve fiziksel Android kabulü ayrı adımlardır.

Faz 1–10 backend ve production hardening uygulanmıştır; frontend ve production deployment ayrı açık talimat gerektirir.

## Faz 10 doğrulama

```powershell
dotnet restore PayAhead.sln
dotnet format whitespace PayAhead.sln --no-restore --verify-no-changes
dotnet build PayAhead.sln --no-restore
dotnet test PayAhead.sln --no-build --logger trx
dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~HardeningIntegrationTests
dotnet list PayAhead.sln package --vulnerable --include-transitive --format json > artifacts/audit.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-audit.ps1 artifacts/audit.json
docker build -f docker/Dockerfile -t payahead:phase10-local .
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/local-container-smoke.ps1
```

Container smoke bu makinenin .tools SDK/.env geliştirme SQL/Compose network'ünü kullanır; Testing env, kendi rastgele DB/container, test JWT ve kontrollü proxy header ile gerçekSQL bağlantısıdır. Gerçek TLS/production credential kanıtı değildir. Backup/restore/CHECKDB ve export/delete de aynı izole testte çalışır. Secret env file finally'de kaldırılır; volume/geliştirme verisi silinmez.

`tests/PayAhead.LoadTests` ayrı executable'dır, çözüm testi sayısına dahil değildir. Restore/build sonrası `PAYAHEAD_LOAD_SQLSERVER` yalnızca yerel SQL base connection olarak güvenli environment'tan sağlanır; runner kendi PayAheadLoad_<guid> DB'sini migrate/seed/cleanup eder.1000plan/10000occurrence/1000rule;100/500concurrent HTTP, bounded worker ve unbound inbox ölçer. Çıktı proje çalışma dizinindeki artifacts/phase10-load.json; API+client aynı süreç CPU/memory, SQL ayrı. Gerçek Expo/store çağrısı yoktur, production kapasitesi değildir. Son sonuçlar progress'tedir.

SQL integration sınıfları container belleği için seri, yarış senaryoları açıkça paraleldir. Testcontainers'ın transitif SSH.NET iki High advisory'si test-only2026.0.0 pin'iyle giderildi; net8/EF8 korunur. CI GitHub'da çalıştırılmadı; yerel actionlint tam workflow dosyalarıyla doğrulandı. Docker/actionlint komutları [DEPLOYMENT.md](DEPLOYMENT.md), privacy [DATA_PRIVACY.md](DATA_PRIVACY.md).

## Faz 9 — Premium geliştirme ve doğrulama

```powershell
dotnet restore PayAhead.sln
dotnet build PayAhead.sln --no-restore
dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
dotnet test tests/PayAhead.UnitTests --filter FullyQualifiedName~PremiumTests
dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~PremiumIntegrationTests
dotnet test PayAhead.sln --no-build
# Ayrı, deployment'a dahil olmayan Kestrel test executable:
dotnet restore tests/PayAhead.PremiumSmokeHost
dotnet build tests/PayAhead.PremiumSmokeHost --no-restore
# Repository kökünden, bu makinedeki .tools SDK ve .env Compose SQL ile:
powershell -NoProfile -ExecutionPolicy Bypass -File tests/PayAhead.PremiumSmokeHost/smoke.ps1
```

SQL testlerinde PAYAHEAD_TEST_SQLSERVER unset ise gerçek Testcontainers SQL Server 2022 başlar; SQLite/InMemory kullanılmaz. Faz 9 testleri gerçek merkezi entitlement policy ile çalışır. Önceki fazların izole regresyon fixture'ları yalnızca test assembly'sinde yüksek limit policy kullanır; eski CRUD/worker senaryolarını Free sınırı değiştirmez. Gerçek Free/Premium API, üç kaynak limit yarışı, unique token/transaction, sahiplik FK ve rowversion yeni SQL testlerindedir.

Kestrel testi kendi PayAheadSmoke_<guid> DB'sini migrate eder; gerçek register/login JWT ile Free 5/6 reddi, test sağlayıcısıyla Premium altıncı plan, expiry, restore, cross-user409, replay202 ve tek completed inbox satırını doğrular. Kaynaklar tests/PayAhead.PremiumSmokeHost ve PayAhead.Testing içinde; test key CSPRNG'dir. Bu makinenin Git dışı `.tools/phase9-smoke.ps1` scripti yalnızca kendi sürecini/geçici DB'sini temizler. Production environment test host tarafından reddedilir. Gerçek store credentials olmadan bu sonuç canlı satın alma değildir.

403 `entitlement_limit`: aktif kaynak sayımı ve planı kontrol edin; silme yerine gereksiz kaynakları pasifleştirin. 409: son rowversion veya satın alma sahipliği. 400: proof/product/environment/account eşleşmesi. 503: store enable, credential/keyring, TLS ve provider erişimi. Webhook ilerlemiyorsa WorkerEnabled, inbox AttemptCount/NextAttemptAtUtc/ErrorCode ve immutable binding kontrol edilir. Gerçek token/body/secret loglamayın. Kurulum ve sınırlar [STORE_INTEGRATION.md](STORE_INTEGRATION.md), istemci sözleşmesi [PREMIUM.md](PREMIUM.md).

VS Code çalışma alanını kök dizinden açın. `global.json` yalnızca .NET 8 SDK 8.0.4xx patch sürümlerine izin verir, C# 12 sabittir. Proje içindeki SDK kaynak kontrolüne alınmaz. .slnx istenen envanter dosyasıdır; .NET 8 uyumluluğu için .sln kullanılır. Yeni proje eklenirse iki dosyayı da güncelleyin.

Restore/build/test ve API başlatma komutları README içindedir. Testler xUnit kullanır. Birim testleri geliştirme kimliği güvenlik sınırını ve SQL model metaverisini doğrular. Entegrasyon testleri HTTP başlangıcı ve gerçek SQL Server migration/constraint/transaction/readiness davranışını doğrular. Model metaverisi testi gerçek veritabanı testi yerine geçmez.

SQL Server testinde SQLite veya EF InMemory kullanılmaz. Testcontainers.MsSql 4.8.1 varsayılan olarak SQL Server 2022 Linux container'ı oluşturur. PAYAHEAD_TEST_SQLSERVER verilirse harici gerçek SQL Server kullanılabilir. Her iki yolda da test rastgele adlandırılmış kendi veritabanını oluşturur ve siler; ortak geliştirme/üretim veritabanı kullanılmaz. Docker yoksa varsayılan test başarısız olur, atlanmaz. `AddInMemoryCollection` kullanımları yalnızca yapılandırma içindir; veritabanı sağlayıcısı değildir.

Her faz sonunda `docs/IMPLEMENTATION_PROGRESS.md` gerçek komut ve sonuçlarla güncellenir. Başarısız/atlanmış testler başarılı sayılmaz. SQL bağımlılığı yoksa engel açıkça kaydedilir.

## Docker geliştirme akışı

README'deki Docker bölümünü izleyin: `.env.example` → yerel `.env`, `docker compose --env-file .env -f docker/compose.yml up -d --wait`, ConnectionStrings__PayAhead ayarı, mevcut migration'ın uygulanması ve API başlatma. `stop sqlserver` verileri korur; volume silmeyin. Host 14333 portu doluysa `.env` portunu değiştirin.

`dotnet test tests/PayAhead.UnitTests --no-build` birim testlerini, `dotnet test tests/PayAhead.IntegrationTests --no-build` entegrasyonu çalıştırır. Testcontainers kendi rastgele host portunu seçer. İlk çalışmada SQL imajı ve Ryuk kaynak temizleme imajı indirilir. Docker Engine erişimi ve registry ağı gerekir; Ryuk'u devre dışı bırakmayın.

API pipeline testleri üretim merkezi hata middleware'ini 500 ve 409 senaryolarında kontrol eder; yanıtlar ProblemDetails olur, iç hata mesajı/stack trace dönmez. Regresyon API testleri yalnızca test assembly içindeki handler ile çalışır. Gerçek JWT testleri shipping API içinde DevelopmentIdentity ile erişim açılamadığını ve Production/Staging bayrak engelini doğrular. Faz 2 ödeme endpoint testleri gerçek SQL Server'da CRUD, izolasyon, concurrency, filtre ve hata sözleşmelerini kontrol eder.

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

## Dashboard/upcoming/calendar doğrulaması

`dotnet test tests/PayAhead.UnitTests --no-build --filter FullyQualifiedName~OverviewTests` ortak projeksiyon, decimal toplamlar, state merge, timezone ve kapasite sınırlarını çalıştırır. `dotnet test tests/PayAhead.IntegrationTests --no-build --filter FullyQualifiedName~OverviewApiTests` gerçek SQL Server API, sahiplik, query sayısı, precision ve limit testleridir. Tüm eski testler için `dotnet test PayAhead.sln --no-build` çalıştırılır.

Saat testte TimeProvider ile sabittir; Europe/Istanbul ve America/Los_Angeles gece sınırı, eksik/geçersiz timezone ve UTC fallback doğrulanır. Üretim kodu sunucunun yerel saatine güvenmez. Calendar year/month zorunludur; 9999 Aralık dahil DateOnly sınırları korunur. API.md varsayılan +30 dahil gün ile tam 7/+6 veya 30/+29 günlük pencere ayrımını açıklar.

10/100/1000 günlük plan ile 30/365 günlük aralıklar birim testinde değerlendirilir. 1000×365 birleşik sonuç sınırını aştığı için açık hata beklenir; 1000×30 kabul edilir. Yerel Debug Stopwatch ve mevcut thread allocation gözlemleri TRX çıktısına yazılır; bunlar isolated benchmark/peak memory/production kapasitesi değildir. Gerçek SQL testinde aynı üç plan hacmi için upcoming'in üç, dashboard'un dört query kullandığı DbCommandInterceptor ile sayılır. Snapshot veri sayısı ve DTO üretimi limitlidir; 50.000 kaynak kayıt + 50.000 birleşik key, geçici dictionary/sıralama buffer'ları yine bellek kullanır. Status veya küçük pageSize limitleri atlatmaz.

Dashboard UpcomingPaymentCount bugün ve sonrasındaki Pending, PendingPaymentCount aralıktaki tüm Pending'dir; aynı alanın iki adı değildir. Calendar gün toplamları Pending borcudur; Paid ActualAmount ayrı dashboard toplamında yer alır. Pasif planın Paid/Skipped kayıtları korunur, modelde deactivation zamanı bulunmadığından geçmiş cutoff tahmin edilmez. Currency geçmiş snapshot'ı da yoktur; fiyat/para birimi sonradan değişirse current-parent metadata sınırı geçerlidir. Salt okuma aşaması kayıt üretmez veya eski veriyi düzeltmez.

## Faz 6 — JWT ile yerel akış

SQL için yukarıdaki Compose ve ConnectionStrings__PayAhead adımlarını kullanın; migration otomatik uygulanmaz:

```powershell
dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:DevelopmentIdentity__Enabled='false'
# Kalıcı geliştirme anahtarı: Base64 ile en az 32 CSPRNG byte; gerçek değeri paylaşmayın.
$keyBytes=New-Object byte[] 64
$rng=[Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($keyBytes); $rng.Dispose()
$env:Jwt__SigningKey=[Convert]::ToBase64String($keyBytes)
$env:Jwt__Issuer='PayAhead'
$env:Jwt__Audience='PayAhead.Mobile'
dotnet run --project src/PayAhead.Api --no-build --no-launch-profile --urls http://localhost:5080
```

Bu makinede `dotnet` yerine `.tools/dotnet/dotnet.exe` (SDK 8.0.426), DOTNET_ROOT=.tools/dotnet ve DOTNET_CLI_HOME=.tools/cli kullanılır. PowerShell 5 için RNG Fill yerine Create().GetBytes($keyBytes) kullanılabilir. Development anahtarı verilmezse yalnızca süreç ömründe geçerli geçici anahtar üretilir. Production/Staging eksik/zayıf secret ile başlamaz; secret manager üzerinden Jwt__SigningKey sağlayın. AllowedHosts ve HTTPS/TLS terminasyonunu dağıtıma göre ayarlayın. TrustedProxies__0 gibi yalnızca açık proxy IP adreslerini verin; allowlist yoksa forwarded headers kapalıdır. Güvenlik sınırları [SECURITY.md](SECURITY.md).

Ayrı PowerShell terminalinde; token içeren değişkenleri/yanıtları ekrana yazdırmayın:

```powershell
$base='http://localhost:5080'
# Yerel örnek parola; üretim hesabında benzersiz parola kullanın.
$account=@{email='local@example.com';password='LocalExamplePassword123!';displayName='Local'}
$null=Invoke-RestMethod -Method Post "$base/api/auth/register" -ContentType application/json -Body ($account|ConvertTo-Json)
$session=Invoke-RestMethod -Method Post "$base/api/auth/login" -ContentType application/json -Body (@{email=$account.email;password=$account.password}|ConvertTo-Json)
$headers=@{Authorization="Bearer $($session.accessToken)"}
$me=Invoke-RestMethod "$base/api/users/me" -Headers $headers
$payments=Invoke-RestMethod "$base/api/payments" -Headers $headers
$session=Invoke-RestMethod -Method Post "$base/api/auth/refresh" -ContentType application/json -Body (@{refreshToken=$session.refreshToken}|ConvertTo-Json)
$null=Invoke-RestMethod -Method Post "$base/api/auth/logout" -ContentType application/json -Body (@{refreshToken=$session.refreshToken}|ConvertTo-Json)
Remove-Variable session,headers,account
```

Eski README seed/development identity akışı kaldırıldı; JWT bypass yoktur. Swagger Authorize ile access token girilebilir. Birim: `dotnet test tests/PayAhead.UnitTests`; gerçek SQL: `dotnet test tests/PayAhead.IntegrationTests`; yalnızca auth: `dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~AuthApiTests`. SQL testleri Docker Testcontainers ve rastgele PayAheadTest_<guid> DB kullanır; mevcut geliştirme verileri silinmez. Tüm çözüm için restore/build/test çalıştırın.

401: issuer/audience/key, expiration, aktif kullanıcı ve migration durumunu kontrol edin. 409 register: normalize email mevcut olabilir. 429: IP bütçesi dolmuştur, Retry-After bekleyin. Beş yanlış parola sonrası doğru parola da kilit süresince reddedilir. Reuse sonrası yeni refresh de 401 ise yeniden login gerekir; paralel refresh göndermeyin. Readiness 503 için migration/SQL kontrol edin. Token/parola/connection string içeren çıktıları paylaşmayın.

## Faz 7 — Worker ve push geliştirme

Önce migration: `dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api`. Notifications varsayılan disabled olduğundan gerçek push veya fake başarı üretilmez. JWT ile reminder/device/settings HTTP akışını provider olmadan deneyebilirsiniz. Örnekler ve alanlar [NOTIFICATIONS.md](NOTIFICATIONS.md) içindedir. Gerçek gönderim için Notifications__Enabled=true, güvenli dış Notifications__AccessToken ve Expo projesinde APNs/FCM yapılandırması gerekir; endpoint varsayılanları Expo HTTPS send/getReceipts, PollSeconds 30 (5–300).

```powershell
dotnet test tests/PayAhead.UnitTests --filter FullyQualifiedName~NotificationTests
dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~NotificationIntegrationTests
dotnet test PayAhead.sln
```

Bu makinede `.tools/dotnet/dotnet.exe`, DOTNET_ROOT=.tools/dotnet, DOTNET_CLI_HOME=.tools/cli ve Docker bin PATH kullanılır. SQL integration Testcontainers rastgele test DB oluşturur; geliştirmenin DB/volume verilerini silmez. Test assembly içindeki FakePushNotificationSender gerçek worker/SQL akışını provider bağımsız sınar; Expo ticket/receipt adapter için kontrollü HttpMessageHandler yanıtları kullanılır. Bu sonuçlar gerçek fiziksel push teslimatı değildir.

Worker açılmıyorsa önce Enabled ve credential/HTTPS ayarlarını, readiness ve pending migration durumunu kontrol edin. Processing takılıysa lease expiry (2 dakika), en eski NextAttemptAtUtc, LastErrorCode ve worker çevrim loglarını inceleyin. 429 için Retry-After/backoff, Failed için credential/payload veya deneme sınırı, AwaitingReceipt için 15 dakika polling ve 24 saat belirsizlik sınırını kontrol edin. Token/parola/provider response çıktılarını paylaşmayın. Log ve güvenli SQL tanılama örnekleri NOTIFICATIONS.md içindedir.

## Faz 8 — Subscription geliştirme ve doğrulama

Önce migration: `dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api`. Yeni tablonun adı TrackedSubscriptions; UserSubscriptions Premium modeli değildir diye yeniden adlandırılmadı, entitlement için korundu. Server startup şema değiştirmez. DTO/örnek/lifecycle [SUBSCRIPTIONS.md](SUBSCRIPTIONS.md) içinde.

```powershell
dotnet test tests/PayAhead.UnitTests --filter FullyQualifiedName~SubscriptionTests
dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~SubscriptionIntegrationTests
dotnet test PayAhead.sln
```

Bu makinede `.tools/dotnet/dotnet.exe` SDK8.0.426 ve mevcut DOTNET_ROOT/CLI_HOME ayarları kullanılır. SQL testleri Testcontainers ve rastgele PayAheadTest_<guid> DB kullanır. Kestrel smoke gerçek JWT ve kendi geçici SQL DB ile register/login→trial create→expiring→update→summary→cancel→cross-user akışıdır. Worker trial fake sender testi gerçek kuyruk/lease kullanır; gerçek Expo teslimatı değildir.

400 için tarih ilişkilerini (FirstBillingDate > TrialEndDate), recurrence/currency ve body biçimini kontrol edin.409 için son rowVersion ve terminal lifecycle durumunu okuyun; generic payment üzerinden bağlı parent metadata değiştirilemez. Trial saat/gün farkları için response.context timezone/fallback alanına bakın. Cancel otomatik provider iptali veya Paid işaretleme yapmaz. Notifications default disabled ve credential gereksinimleri değişmedi.
## Phase 5.2 — Development çalıştırma

Dil migration'ı bu makinenin doğrulanmış Docker Development hedefine uygulanmıştır. Yeni makinede önce hedef DB/uygulanmış migration listesini doğrulayın, EF Core 8 ile açıkça migration uygulayın; bağlantı bilgilerini User Secrets/environment içinde tutun. [Şema ve dil rehberi](LOCALIZATION.md).

```powershell
$env:DOTNET_CLI_HOME="$PWD/.tools/cli"
.tools/dotnet/dotnet.exe restore PayAhead.sln --artifacts-path artifacts/phase52-check
.tools/dotnet/dotnet.exe build PayAhead.sln --no-restore --artifacts-path artifacts/phase52-check
.tools/dotnet/dotnet.exe test PayAhead.sln --no-build --artifacts-path artifacts/phase52-check
.tools/dotnet/dotnet.exe ef migrations list --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
# ConnectionStrings__PayAhead güvenli süreç ortamında seçildikten ve hedef doğrulandıktan sonra:
.tools/dotnet/dotnet.exe ef database update 20261025154307_CatalogLocalization --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
.\scripts\Start-Development.ps1 -Port 5080
```

Eski API terminalini bu işlem otomatik durdurmaz. Yeni API'yi başlatmadan önce aynı porttaki eski süreci kendi terminalinden kontrollü kapatın veya boş bir port seçin. Çalışan eski binary 14 dili kabul etmeyebilir; salt migration güncel API kodunun yerine geçmez. SQL testi Testcontainers ve ayrı rastgele DB kullanır, mevcut volume silinmez.
