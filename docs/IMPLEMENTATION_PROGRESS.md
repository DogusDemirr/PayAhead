# Uygulama ilerlemesi

## Faz 1 — Çözüm temeli

Tarih: 21 Ekim 2026 (Europe/Istanbul). Durum: **Faz 1 tamamlandı**. Faz 2 başlatılmadı; açık kullanıcı onayı bekleniyor.

### İnceleme ve plan

Başlangıç deposu boştu; korunacak uygulama veya mevcut AGENTS.md bulunmadı. Makinede yalnızca .NET 9 SDK vardı, .NET 8 runtime mevcuttu. Docker komutu bulunamadı. MSSQLSERVER servisi çalışıyordu. Resmi kurulum betiğiyle .NET SDK 8.0.426 proje içindeki, kaynak kontrolünden hariç `.tools/dotnet` klasörüne kuruldu. Tüm derleme/doğrulamalar bu SDK ile yapıldı; projeler .NET 9/10'a yükseltilmedi.

Plan sırası: proje iskeleti → SQL modeli → migration → API altyapısı → testler → dokümantasyon ve duruş.

### Uygulananlar

- Clean Architecture için dört uygulama ve iki test projesi; net8.0 ve C# 12 sabitlemesi.
- DI, FluentValidation kayıt sınırı, ICurrentUser sözleşmesi.
- On temel varlık, SQL Server DbContext ve Fluent API konfigürasyonları.
- FK, unique/check constraint, decimal(19,4), date/time, rowversion ve başlangıç indeksleri.
- Serilog, merkezi hata yönetimi, ProblemDetails, kimlik gerektiren varsayılan politika.
- Liveness, SQL/migration readiness ve Development OpenAPI/Swagger.
- Varsayılan kapalı, Development dışı ortamda başlangıcı reddeden geliştirme kimliği.
- JWT kabulü Faz 6'ya kadar kapalı; kayıt/giriş veya iş endpoint'i eklenmedi.
- xUnit testleri, VS Code görevleri, Docker paketleme başlangıcı ve Türkçe geliştirici dokümanları.

### Eklenen dosyalar

Depo başlangıçta boş olduğundan aşağıdaki dosyaların tamamı yenidir:

- Kök: `.gitignore`, `.dockerignore`, `global.json`, `Directory.Build.props`, `PayAhead.sln`, `PayAhead.slnx`, `README.md`, `AGENTS.md`.
- Araç/editör: `.config/dotnet-tools.json`; `.vscode/tasks.json`, `launch.json`, `extensions.json`.
- Domain: `PayAhead.Domain.csproj`, `Entities.cs`.
- Application: `PayAhead.Application.csproj`, `DependencyInjection.cs`.
- Infrastructure: `PayAhead.Infrastructure.csproj`, `PayAheadDbContext.cs`, `EntityConfigurations.cs`, `DependencyInjection.cs`, `DesignTimeDbContextFactory.cs`.
- Infrastructure/Persistence/Migrations: `20261021084857_InitialCreate.cs`, ilgili `.Designer.cs`, `PayAheadDbContextModelSnapshot.cs`.
- API: `PayAhead.Api.csproj`, `Program.cs`, `DevelopmentIdentity.cs`, `ApiExceptionHandler.cs`, `DatabaseHealthCheck.cs`, `HealthDocumentFilter.cs`, `appsettings.json`, `appsettings.Development.json`.
- tests/PayAhead.UnitTests: proje dosyası ve `FoundationTests.cs`.
- tests/PayAhead.IntegrationTests: proje dosyası ve `FoundationIntegrationTests.cs`.
- docker: `Dockerfile`, `compose.yml`.
- docs: `ARCHITECTURE.md`, `DATABASE.md`, `API.md`, `DEVELOPMENT.md`, `DEPLOYMENT.md`, `IMPLEMENTATION_PROGRESS.md`, `PRODUCTION_CHECKLIST.md`.

### Veritabanı değişiklikleri

InitialCreate on iş tablosunu ve EF migration geçmişini oluşturur. Gerçek yerel SQL Server üzerinde rastgele `PayAheadTest_<guid>` veritabanına uygulandı. Test sonunda yalnızca testin kendi oluşturduğu veritabanı silindi. Mevcut geliştirme/üretim veritabanına migration veya yıkıcı işlem uygulanmadı. API açılışta otomatik migration çalıştırmaz.

### Çalıştırılan komutlar ve gerçek sonuçlar

Aşağıda `dotnet` bu oturumda `.tools/dotnet/dotnet.exe` olarak çalıştırıldı. `DOTNET_CLI_HOME` proje içindeki `.tools/cli`; EF komutlarında `DOTNET_ROOT` proje içindeki SDK klasörüdür.

| Komut/kontrol | Sonuç |
| --- | --- |
| Depo dosya listesi, dotnet --info, Get-Service *SQL*, docker --version | Boş depo, başlangıçta SDK 9.0.201, çalışan MSSQLSERVER; Docker yok |
| Resmi dotnet-install.ps1 -Channel 8.0 -InstallDir .tools/dotnet -NoPath | SDK 8.0.426 kuruldu |
| dotnet new sln -n PayAhead; dotnet sln PayAhead.sln add ... | Altı proje çözüme eklendi |
| dotnet new tool-manifest; dotnet tool install dotnet-ef --version 8.0.21 | EF aracı yerel manifeste sabitlendi |
| dotnet ef migrations add InitialCreate --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --output-dir Persistence/Migrations | Başarılı; migration ve snapshot üretildi |
| dotnet restore PayAhead.sln (son çalıştırma) | Başarılı; tüm projeler güncel |
| dotnet build PayAhead.sln --no-restore (son çalıştırma) | Başarılı; 0 uyarı, 0 hata |
| dotnet test PayAhead.sln --no-build (PAYAHEAD_TEST_SQLSERVER ayarlı) | 9 birim + 2 entegrasyon başarılı; 0 başarısız, 0 atlanan |
| dotnet ef migrations has-pending-model-changes --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build | Model ve migration uyumlu; değişiklik yok |
| dotnet list PayAhead.sln package --vulnerable --include-transitive | Altı proje için sorgulanan kaynaklarda bilinen açık bildirilmedi |

SQL testinde parola içermeyen Windows Integrated Security bağlantısı kullanıldı. Test gerçek SQL Server'da migration, decimal hassasiyeti, DateOnly round-trip, rowversion çatışması, benzersiz occurrence kısıtı, foreign key, negatif tutar check constraint, transaction rollback ve API readiness 200 kontrol etti. Diğer HTTP testi WebApplicationFactory ile API başlangıcı, liveness 200, OpenAPI'de iki sağlık ucunun varlığı ve yetkisiz ProblemDetails yanıtını kontrol etti. Bu, Docker veya yük testi kanıtı değildir.

### Karşılaşılan ve çözülen sorunlar

- İlk SDK indirme denemesi sandbox ağ erişimine takıldı; izinli tekrar başarılı oldu. Betik çalıştırma politikası nedeniyle yalnızca kurulum işlemi için ExecutionPolicy Bypass kullanıldı.
- İlk CLI/NuGet araç çağrıları profil dosya izinlerine takıldı; izinli çalıştırma ve proje içi CLI diziniyle çözüldü.
- Sandbox içindeki SQL testinde Windows şifreleme altyapısına erişim hatası oluştu; izinli gerçek SQL çalıştırmasıyla doğrulandı. Şifreleme kapatılmadı.
- İlk izinli SQL testinde readiness 503 döndü: DI bağlantıyı test yapılandırması tamamlanmadan yakalıyordu. DbContext yapılandırmayı çözümleme anında alacak şekilde düzeltildi; API'nin gerçekten test veritabanına bağlandığı ayrıca assert edildi. Son tam test çalıştırması başarılıdır.

### Bilinen sınırlamalar ve sonraki faz

- Docker kurulu değil: Dockerfile ve Compose çalıştırılmadı. Testcontainers henüz eklenmedi; SQL doğrulaması gerçek yerel SQL Server ile yapıldı.
- .NET 8 CLI `.slnx` desteklemez; istenen envanter yanında uyumlu `.sln` bulunur. Derleme/test için `.sln` kullanılır.
- Veritabanı entegrasyon testi create/drop yetkisi gerektirir; PAYAHEAD_TEST_SQLSERVER verilmezse açıkça atlanır.
- Kapsam Faz 1 ile sınırlıdır. Gerçek JWT/refresh güvenliği, CRUD, finans/yinelenme kuralları, bildirimler, premium, export/silme ve CI/yük/üretim sertleştirmesi sonraki fazlardadır.
- .NET 8 desteği 10 Kasım 2026'da sona erer. Üretimden önce açık onayla desteklenen LTS'ye geçiş zorunlu plan maddesidir; kaynak ve adımlar DEPLOYMENT.md içindedir.
- Uygulama için üretim güvenliği veya hukuki uygunluk iddiası yoktur.

**Sonraki adım:** Kullanıcının açık onayından sonra Faz 2 — Payment Plan CRUD. Bu oturum Faz 1 sonunda durduruldu.

## Faz 1 ek doğrulama — Docker ve SQL Server

Tarih: 21 Ekim 2026. Durum: **İstenen Docker/SQL Server doğrulaması tamamlandı. Faz 2 başlatılmadı.** Yukarıdaki Docker bulunmadığına ilişkin ifadeler ilk çalışmanın tarihsel kayıtlarıdır; güncel sonuçlar bu bölümdedir.

### Ortam ve korunan yapı

- Docker 29.9.1, Engine çalışıyor; context desktop-linux, Linux/x86_64, Compose 5.6.0.
- `docker --version`, `docker info`, `docker compose version` kurulu CLI'nin tam yolu/oturum PATH'i kullanılarak çalıştırıldı. Başlangıçta mevcut terminal PATH'i yeni kurulumu görmüyordu. İlk dizin erişimi sandbox tarafından engellendi; izinli tekrar başarılı oldu.
- Sistem `dotnet --list-sdks`: yalnızca 9.0.201. Proje `.tools/dotnet/dotnet --list-sdks`: **8.0.426**. Restore/build/test için yalnızca proje .NET 8 SDK'sı kullanıldı.
- Tüm projeler Directory.Build.props üzerinden net8.0/C# 12; EF Core 8.0.21. Mimari ve mevcut migration dosyaları değişmedi.
- Başlangıçta başka Docker container'ı yoktu. Yerel Windows SQL Server servisi durdurulmadı; Compose için boş 14333 portu kullanıldı.

### Eklenen ve değiştirilen dosyalar

- `docker/compose.yml`: proje adı, loopback 14333 portu, healthcheck, hostname, restart politikası; mevcut named volume korundu.
- `.env.example`: boş parola alanı ve geliştirme portu. Yerel `.env` rastgele parola ile oluşturuldu, `git check-ignore .env` başarılı; Git tarafından izlenmiyor. Gerçek parola doküman veya kaynak dosyasına yazılmadı.
- `tests/PayAhead.IntegrationTests/PayAhead.IntegrationTests.csproj`: Testcontainers.MsSql 4.8.1.
- `SqlServerFixture.cs`: otomatik izole SQL container'ı, dinamik port/geçici parola ve kaynak temizliği; isteğe bağlı harici SQL bağlantısı.
- `FoundationIntegrationTests.cs`: skip yerine gerçek SQL fixture, insert/query/update/delete, fiziksel şema kontrolleri ve mevcut finans/FK/unique/concurrency/transaction kontrolleri.
- `ApiPipelineTests.cs`: 500/409 ProblemDetails, gerçek API host'unda Development kimliği ve Production/Staging reddi.
- `README.md`, `docs/DATABASE.md`, `docs/DEVELOPMENT.md`, bu ilerleme dosyası: başlangıç/durdurma, bağlantı, migration, test, sorun giderme ve gerçek sonuçlar.
- `AGENTS.md` ve `docs/DEPLOYMENT.md`: güncel test davranışı ve Docker durumu düzeltildi.

### Container ve veritabanı sonucu

Compose container'ı **payahead-phase1-sqlserver-1**, named volume **payahead-phase1_sql-data**, bağlantı **127.0.0.1:14333**, veritabanı **PayAheadDockerDevelopment**. Son durumda container çalışıyor ve healthy. Kaynaklar geliştirme için bırakıldı; doğrulama için başlatılan host API işlemi HTTP kontrollerinden sonra durduruldu. Testcontainers geçici SQL/Ryuk kaynaklarını temizledi.

İmaj: `mcr.microsoft.com/mssql/server:2022-latest`; bu çalışmadaki digest `sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718`. Sorgulanan sürüm SQL Server 2022 CU27-GDR **16.0.4305.5**, Ubuntu 22.04.5. Desteklenen SQL Server Linux container kullanımına ilişkin [Microsoft belgeleri](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?pivots=cs1-bash&tabs=cli) ve [Testcontainers SQL modülü](https://dotnet.testcontainers.org/modules/mssql/) esas alındı. `2022-latest` değişebilir; digest bu çalışmanın kaydıdır.

`dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build` başarıyla mevcut InitialCreate migration'ını uyguladı. Sonrasında Compose restart + up --wait başarılı oldu; aynı veritabanı volume üzerinde korundu. Mevcut başka veritabanı veya migration silinmedi.

Oluşan tablolar: **Users, PaymentPlans, PaymentOccurrences, ReminderRules, UserSettings, DeviceTokens, UserSubscriptions, RefreshTokens, NotificationDeliveries, AuditLogs** ve EF'nin **__EFMigrationsHistory** tablosu. Fiziksel SQL kataloglarında on iş tablosunun PK'leri, on etkin/trusted FK, Amount decimal(19,4), iki sekiz baytlık rowversion alanı ve occurrence unique index doğrulandı.

### Çalıştırılan kontroller ve son sonuçlar

| Kontrol/komut | Gerçek sonuç |
| --- | --- |
| Docker sürüm/info/compose ve iki SDK listesi | Başarılı; Engine Linux; gerçek .NET 8 SDK mevcut |
| `docker compose --env-file .env -f docker/compose.yml config --quiet` | Başarılı; secrets ekrana yazılmadı |
| `docker compose ... up -d --wait --wait-timeout 180` | Başarılı; SQL healthy |
| `dotnet restore PayAhead.sln` | Başarılı; yeni Testcontainers bağımlılıkları geri yüklendi |
| `dotnet build PayAhead.sln --no-restore` (son) | Başarılı; 0 uyarı, 0 hata |
| `dotnet test PayAhead.sln --no-restore --logger trx;LogFilePrefix=phase1-docker` (son) | 9 birim + 7 entegrasyon başarılı; 0 başarısız, 0 atlanan |
| EF database update | Başarılı; Docker geliştirme veritabanı oluşturuldu |
| API `dotnet run ... --urls http://localhost:5080` | Başarıyla başladı; Docker SQL bağlantısı readiness ile doğrulandı |
| HTTP /health/live, /health/ready | İkisi de 200 JSON |
| HTTP /swagger/index.html, /swagger/v1/swagger.json | İkisi de 200 |
| Compose restart, up --wait, sqlcmd katalog sorguları | Başarılı; volume kalıcılığı, tablolar ve ilişkiler doğrulandı |

### Tek tek test kapsamı

| Test | Gerçek çalıştırma |
| --- | --- |
| Development_identity_is_rejected_outside_development (Production/Staging/Development boşluklu) | 3/3 başarılı |
| Development_identity_is_disabled_by_default | 1/1 başarılı |
| Development_identity_requires_explicit_valid_user (null/hatalı/boş Guid) | 3/3 başarılı |
| Development_identity_uses_only_configured_user | 1/1 başarılı |
| Sql_server_model_has_financial_precision_and_unique_occurrence_index | 1/1 başarılı; model metaverisi testi |
| Api_starts_and_liveness_is_public | 1/1 başarılı; HTTP/OpenAPI/401 ProblemDetails |
| Migration_constraints_transactions_and_readiness_work_on_real_sql_server | 1/1 başarılı; gerçek ayrı SQL Server Testcontainers container'ında migration, fiziksel şema, PaymentPlan insert/query/update/delete, decimal, FK, unique, rowversion, check constraint, rollback ve readiness |
| Production_exception_middleware_returns_safe_problem_details (500/409) | 2/2 başarılı; production middleware ve gerçek handler ile ProblemDetails, iç ayrıntı gizleme |
| Actual_api_refuses_development_identity_outside_development (Production/Staging) | 2/2 başarılı; gerçek API host başlangıcı reddedildi |
| Actual_api_can_explicitly_enable_development_identity | 1/1 başarılı; gerçek Development API host'unda scheme ve health |

Son TRX raporları test projelerinin `TestResults/phase1-docker_net8.0_20261021122758.trx` (birim) ve `TestResults/phase1-docker_net8.0_20261021122811.trx` (entegrasyon) dosyalarında; Git dışında tutulur. Yedi entegrasyon vakasından birisi çok sayıda gerçek SQL senaryosunu birlikte doğrular; her bir SQL assert ayrı xUnit testi olarak sayılmamıştır.

### Ara hatalar ve sınırlar

- NuGet.Config sandbox izin hatası izinli tekrar ile çözüldü.
- Docker credential helper PATH'te görünmüyordu; yalnızca işlem PATH'i düzeltildi, sistem PATH'i değiştirilmedi.
- Yeni hata testi ilk derlemede lambda/throw sözdizimi hatası verdi; düzeltildi.
- İlk şema testi SQL collation sırasını ordinal sıra sanıyordu; iki liste ordinal sıralanarak düzeltildi. Şemada hata yoktu.
- API çalışırken yeniden derleme Windows DLL kilidine takıldı. Bu oturumun API işlemi kapatıldı; son build sıfır uyarı/hata ile geçti.
- Son doğrulamada açık hata veya atlanan test yoktur. Docker Engine'e erişemeyen ortamda Testcontainers testi başarısız olur; SQLite/InMemory'ye düşmez.
- API container imajının build/run testi yapılmadı; bu görevde istenen SQL container'ı ve host API doğrulandı. Üretim sertleştirmesi, CI/yük testleri ve sonraki iş fazları bu görevin kapsamı dışındadır.

**Duruş:** Faz 1 Docker/SQL doğrulaması sonunda duruldu. Faz 2 veya başka geliştirme için açık onay bekleniyor.

## Faz 2 — Payment Plan CRUD

Tarih: 21 Ekim 2026 (Europe/Istanbul). Durum: **Faz 2 tamamlandı. Faz 3 başlatılmadı; açık onay bekleniyor.**

### İnceleme ve uygulama planı

Depo, AGENTS.md, önceki ilerleme kaydı, Domain varlıkları, Fluent API konfigürasyonları, InitialCreate/snapshot, API kimliği ve ProblemDetails altyapısı, gerçek SQL Server Testcontainers testleri okundu. Başlangıç çalışma ağacında önceki Faz 1 Docker doğrulamasının commit edilmemiş değişiklikleri vardı; korundu. SDK 8.0.426, net8.0/C# 12 ve EF Core 8.0.21 doğrulandı. Yeni NuGet paketi eklenmedi, .NET 9/10'a geçilmedi.

Plan sırası: DTO/doğrulama ve domain davranışı → Application kullanım senaryoları → kullanıcıya özel SQL persistence → controller ve hata eşleme → birim/gerçek SQL/HTTP testleri → dokümantasyon ve duruş.

### Uygulanan endpoint'ler

| Uç | Davranış |
| --- | --- |
| POST /api/payments | Sahipliği sunucudan alan kayıt; 201, Location, ETag ve DTO |
| GET /api/payments | Kullanıcıya özel sayfalama/filtreler; 200 PagedResult |
| GET /api/payments/{id} | Sahip olunan DTO ve ETag; yok/başka kullanıcı 404 |
| PUT /api/payments/{id} | Tam güncelleme; body rowVersion ile SQL concurrency; 200 veya 409 |
| DELETE /api/payments/{id} | If-Match strong ETag; ilişkisiz plan için 204, geçmiş/hatırlatma varsa 409 |

### Kurallar, güvenlik ve mimari

- Name boş değil ve <=150, Description <=2000, Category boş değil ve <=50. Amount pozitif, en fazla decimal(19,4) ve en fazla dört ondalık basamak; sessiz yuvarlama yok.
- CurrencyCode resmi SIX ISO 4217 güncel listesiyle doğrulanır; snapshot yayın tarihi 2026-09-17. Currency trim/invariant uppercase; name/category/description trim edilir. Kodların kaynağı [SIX](https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html); uygulama her istekte ağ çağrısı yapmaz. Snapshot bakım gerektirir.
- Frequency tanımlı string enum; Interval >=1; zorunlu, default olmayan StartDate; EndDate >= StartDate; isteğe bağlı AnchorDay 1–31. Recurrence hesaplaması eklenmedi.
- Pagination varsayılan 1/20, PageSize en fazla 100, PageNumber en fazla 1.000.000. IsActive, Category, CurrencyCode ve Name search AND ile birleşir. Sıra CreatedAtUtc DESC / Id ASC; iki SQL sorgusu, AsNoTracking, N+1 yok. Count/sayfa eşzamanlı değişikliklerde tek snapshot garantisi vermez.
- UserId body'de yoktur; ICurrentUser üzerinden alınır. Sözleşme dışı UserId/CreatedAtUtc gibi alanlar 400 ile reddedilir. Create aktif/mevcut kullanıcı ister. Tüm bulma/listeleme sorguları UserId filtrelidir; başka kullanıcının get/put/delete isteği 404'tür. EF parametreli sorgular kullanılır.
- Domain Create/UpdateDetails temel iş kurallarını uygular. Application DTO/validator/PaymentService/IPaymentPlanStore içerir. Infrastructure SQL sorgularını ve SaveChanges'i uygular. Controller HTTP/ETag eşlemesi yapar; DbContext veya iş kuralı içermez.
- PUT sekiz baytlık canonical Base64 rowVersion ister. SQL UPDATE özgün sürüme koşulludur; aynı içerikli PUT da sürümü kontrol eder. Id/UserId/CreatedAtUtc korunur; UpdatedAtUtc güvenilir TimeProvider ile güncellenir. JSON UTC zamanları Z ile gösterilir.
- DELETE tek strong ETag ister; wildcard/weak/çoklu/eksik token reddedilir. İlişkili occurrence veya reminder varsa 409. Kontrol ile silme arasındaki yarışları FK ve rowversion da korur. Tek SaveChanges atomiktir; geçmişe cascade uygulanmaz. Soft delete eklenmedi; IsActive iş durumudur.
- Doğrulama/model-binding 400, kimlik 401, sahiplik/bulunamama 404, concurrency/bağımlılık 409 ve beklenmeyen 500 ProblemDetails döner. İç hata/SQL/stack trace response'a girmez. Geliştirme kimliğinin Production/Staging engeli korundu; gerçek JWT akışı hâlâ Faz 6 kapsamındadır.

### Bu fazda eklenen/değiştirilen dosyalar

- Domain: `Entities.cs` (PaymentPlan partial), yeni `PaymentPlan.Behavior.cs`.
- Application: `DependencyInjection.cs`; yeni `Payments/Contracts.cs`, `CurrencyCodes.cs`, `Validators.cs`, `PaymentService.cs`.
- Infrastructure: `DependencyInjection.cs`; yeni `PaymentPlanStore.cs`.
- API: `Program.cs`, `ApiExceptionHandler.cs`; yeni `Controllers/PaymentsController.cs`, `PaymentSchemaFilter.cs` (OpenAPI zorunlu alan/sınır açıklamaları).
- Testler: yeni `tests/PayAhead.UnitTests/PaymentTests.cs`, `tests/PayAhead.IntegrationTests/PaymentApiTests.cs`. Önceki test dosyaları korundu.
- Dokümanlar: `README.md`, `docs/API.md`, bu dosya; güncel durumu korumak için `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/DATABASE.md`.
- Yalnızca bu makinedeki Git dışı `.tools/phase2-smoke.ps1`: Kestrel/Compose HTTP doğrulaması ve kendi geçici veritabanını temizleme; ürün kodu değildir. Kalıcı test kapsamı test projelerindedir.

### Veritabanı ve komutlar

Yeni migration gerekmedi. Eski migration/snapshot dosyaları değiştirilmedi veya silinmedi; EF `has-pending-model-changes` çıktısı model değişikliği olmadığını doğruladı. Mevcut nvarchar(200) Name kolonu daraltılmadan API'de 150 sınırı uygulandı; eski veriye dokunulmadı.

Bu tabloda `dotnet`, `.tools/dotnet/dotnet.exe` (SDK 8.0.426) olarak çalıştırıldı. DOTNET_CLI_HOME proje içindeki .tools/cli, DOTNET_ROOT proje içindeki SDK dizinidir; Docker bin dizini yalnızca işlem PATH'ine eklendi.

| Çalıştırılan kontrol | Gerçek son sonuç |
| --- | --- |
| dotnet --version / Docker info | 8.0.426 / Engine 29.9.1 Linux çalışıyor |
| dotnet restore PayAhead.sln | Başarılı; tüm projeler güncel |
| dotnet build PayAhead.sln --no-restore | Başarılı; 0 uyarı, 0 hata |
| dotnet test PayAhead.sln --no-build --logger trx;LogFilePrefix=phase2-final | **56 birim + 37 entegrasyon geçti; 0 başarısız, 0 atlanan** |
| dotnet ef migrations has-pending-model-changes --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build | Model değişikliği yok |
| Kestrel + Compose SQL Server HTTP doğrulaması (son tekrar) | readiness 200; POST 201; LIST/GET/PUT 200; eski sürüm PUT 409; DELETE 204; OpenAPI 200 |

TRX raporları: `tests/PayAhead.UnitTests/TestResults/phase2-final_net8.0_20261021125030.trx`, `tests/PayAhead.IntegrationTests/TestResults/phase2-final_net8.0_20261021125104.trx`. Git dışında tutulurlar. Önceki Faz 1 testleri de bu toplamda gerçek olarak çalıştı ve geçti.

### Test kapsamı ve ara düzeltmeler

56 birim testi: önceki 9 + yeni 47. Yeni kapsam: create/update zorunlu alanları, Name sınırı, tutar hassasiyeti/taşma, ISO üyeliği, enum/interval/tarih/anchor kuralları, rowversion biçimi, sayfa hesabı/taşma, filtre normalleştirme ve kullanıcıya iletimi, domain kimlik/zaman koruması, Application create ve kimlik davranışı. Fake store sadece birim testinde kullanılır; SQL kanıtı sayılmaz.

37 entegrasyon testi: önceki 7 + yeni 30. PaymentApiTests gerçek Testcontainers SQL Server'ında her vaka için yeni `PayAheadTest_<guid>` veritabanı oluşturdu. CRUD round-trip/DTO/Location/ETag/UTC, sahiplik izolasyonu ve overposting reddi, birleşik filtreler/literal yüzde/SQL injection metni, deterministik sıralama, invalid create+update (15 parametreli vaka), invalid query (6 vaka), bozuk JSON/GUID ve eksik token, ilişkili geçmiş/hatırlatmayı koruma (2 vaka), aynı rowversion ile yarışan iki yazar, anonim/olmayan kullanıcı ve OpenAPI required alanları doğrulandı. SQLite/EF InMemory kullanılmadı.

Gerçek HTTP doğrulamasında Compose SQL Server üzerinde yalnızca bu testin `PayAheadSmoke_<guid>` veritabanı oluşturuldu; mevcut migration uygulandı ve kullanılamaz parola işareti olan test kimliği seed edildi. Kestrel ayrı süreçte başladı. Yalnızca betiğin kendi API işlemi ve kendi geçici veritabanı temizlendi; PayAheadDockerDevelopment veya mevcut başka kayıt silinmedi.

Ara sonuçlar gizlenmedi: ilk birim test derlemesinde FluentValidation'ın iki overload'u arasında belirsiz `new()` kullanımı açık tipe çevrildi. İlk entegrasyon turunda yedi vaka ProblemDetails içerik türü tutarsızlığı buldu; genel Produces(application/json) bildirimi kaldırılıp model-binding hatalarının application/problem+json dönmesi sağlandı. Son testler başarılıdır. Son HTTP betiği tekrarında PowerShell UTF-8 BOM, sqlcmd stdin hatası oluşturdu; BOM'suz encoding sabitlendi, adı doğrulanan yalnızca kendi geçici veritabanı temizlendi ve HTTP testi tekrar başarılı çalıştı. Ürün kodunda SQL/migration hatası yoktu.

### Kalan sınırlar ve duruş

- Bu faz için açık derleme/test hatası veya atlanan test yoktur.
- Gerçek kullanıcı kaydı/JWT/refresh Faz 6'dadır. Şimdilik ödeme uçları yalnızca Development kimliğiyle denenebilir; bu üretim güvenliği iddiası değildir.
- ISO listesi sürümlü snapshot'tır; yeni kod değişikliklerinde resmî listeye göre kontrollü güncellenmelidir.
- Recurrence engine, occurrence işlemleri, takvim, premium ve bildirimler eklenmedi.

**Sonraki faz: Faz 3 — Recurrence Engine. Açık kullanıcı onayı gelene kadar başlanmayacak.**

## Faz 3 — Recurrence Engine

Tarih: 21 Ekim 2026 (Europe/Istanbul). Durum: **Faz 3 tamamlandı. Faz 4 başlatılmadı; açık onay bekleniyor.**

### Ön inceleme ve plan

AGENTS.md, bu ilerleme kaydı, PaymentPlan/enum/Domain davranışları, mevcut Application CRUD servisi/DI, önceki birim ve SQL Server testleri, SDK/paket hedefleri incelendi. Çalışma ağacı başlangıçta temizdi. SDK 8.0.426, net8.0/C# 12, EF Core 8.0.21 ve Docker Engine 29.9.1 Linux doğrulandı; sürüm yükseltmesi veya yeni paket eklenmedi.

Plan: saf Domain sözleşmesi/hesaplayıcısı → Application DI → açık tarih beklentileriyle birim testleri → SQL DateOnly round-trip ve eski CRUD regresyonu → performans gözlemleri → dokümantasyon ve duruş.

### Eklenen özellikler ve sınıflar

`PayAhead.Domain.Recurrence.IPaymentRecurrenceCalculator` ve `PaymentRecurrenceCalculator` eklendi. Önerilen iki metot korundu: `CalculateOccurrences(PaymentPlan, fromDate, toDate)` ve `GetNextOccurrence(PaymentPlan, referenceDate)`. Application DI üzerinden singleton çözümlenir. Hesaplayıcı durumsuzdur; DB/EF/HTTP/Controller veya sistem saati kullanmaz. Modelin alanlarını değiştirmez; sadece DateOnly tarihleri döndürür.

- Once: yalnızca StartDate.
- Daily: StartDate + n×Interval gün.
- Weekly: StartDate + n×7×Interval gün.
- Monthly: orijinal başlangıç ayından n×Interval ay.
- Yearly: başlangıç ayını koruyarak n×Interval yıl.
- StartDate her zaman ilk occurrence. Aylık/yıllık sonraki tekrarlarda gün `AnchorDay ?? StartDate.Day`; kısa ayda son geçerli güne kısaltılır ama anchor değiştirilmez. Günlük/haftalık/tek seferlik türlerde geçerli AnchorDay etkisizdir.
- Ayın son günü olma otomatik yapışkan bir özellik değildir: anchor null olan 30 Nisan planı 30 Mayıs üretir; AnchorDay=31 ise 31 Mayıs üretir. StartDate=15 Ocak, AnchorDay=31 planı ilk ayda yalnızca 15 Ocak, sonra 28 Şubat ve 31 Mart üretir.
- 31 Ocak → 28/29 Şubat → 31 Mart; 29 Şubat yıllık planı artık olmayan yılda 28 Şubat'a kısalır ve sonraki artık yılda 29 Şubat'a döner. Gregorian 2100 kuralı test edildi.
- fromDate/toDate ve next referansı dahildir. StartDate öncesi/EndDate sonrası üretilmez. Pasif plan her aralıkta boş liste/next null verir; tarihçe motoru olarak kullanılmaz.
- Geçersiz aralık, interval, enum, anchor veya EndDate: DomainRuleException; null model: ArgumentNullException. Yapısal doğrulama pasif modelde de çalışır.
- Liste başına 10.000 sonuç sınırı; 10.001'inci tarihte açık hata, kısmi sonuç yok. Geniş ama seyrek aralıklar kabul edilir. Next liste oluşturmadan doğrudan ilk tarihi bulur.
- İlk tekrar matematiksel indeksle bulunur; geçmiş bütün tekrarlar dolaşılmaz. Büyük interval çarpımları long ile ve DateOnly sınır kontrolleriyle güvenlidir. Temsil edilemeyen gelecek null ile sonlanır; taşma veya sonsuz döngü oluşmaz.

### Dosyalar

Yeni ürün dosyaları:

- `src/PayAhead.Domain/Recurrence/IPaymentRecurrenceCalculator.cs`
- `src/PayAhead.Domain/Recurrence/PaymentRecurrenceCalculator.cs`

Yeni test dosyaları:

- `tests/PayAhead.UnitTests/RecurrenceTests.cs`
- `tests/PayAhead.IntegrationTests/RecurrenceIntegrationTests.cs`

Değişen dosyalar: `src/PayAhead.Application/DependencyInjection.cs` (Domain servisi kaydı), `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, bu dosya; güncel faz bilgisini korumak için `AGENTS.md`, `README.md` ve `docs/API.md`.

### Veritabanı ve API etkisi

Şema/entity alanı/migration değişmedi. Mevcut veriler veya migration'lar değiştirilmedi. CRUD endpoint/controller/sözleşmeleri korundu; yeni HTTP ucu yoktur. Hesaplayıcı PaymentOccurrence tablosuna kayıt yazmaz. SQL testleri sadece kendi rastgele `PayAheadTest_<guid>` veritabanlarını oluşturup temizledi; geliştirme/üretim veritabanları kullanılmadı.

### Çalıştırılan komutlar ve gerçek sonuçlar

Aşağıdaki dotnet komutları proje içindeki `.tools/dotnet/dotnet.exe` (SDK **8.0.426**) ile çalıştırıldı. DOTNET_CLI_HOME=.tools/cli, DOTNET_ROOT=.tools/dotnet; Docker bin dizini yalnızca oturum PATH'ine eklendi. PAYAHEAD_TEST_SQLSERVER kaldırılarak gerçek Testcontainers yolu kullanıldı.

| Komut | Gerçek sonuç |
| --- | --- |
| dotnet --version; docker info --format ... | 8.0.426; Docker Engine 29.9.1 Linux |
| dotnet test tests/PayAhead.UnitTests --no-restore --filter FullyQualifiedName~RecurrenceTests (ilk tur) | İlk 70 yeni recurrence vakası geçti |
| dotnet restore PayAhead.sln (son) | Başarılı; tüm projeler güncel |
| dotnet build PayAhead.sln --no-restore (son) | Başarılı; 0 uyarı, 0 hata |
| dotnet test PayAhead.sln --no-build --logger trx;LogFilePrefix=phase3-final | **136 birim + 42 entegrasyon başarılı; 0 başarısız, 0 atlanan** |
| git diff --check | Başarılı; whitespace hatası yok |
| Migration dizini git diff kontrolü | Değişiklik yok |

Son TRX dosyaları:

- `tests/PayAhead.UnitTests/TestResults/phase3-final_net8.0_20261021130517.trx`
- `tests/PayAhead.IntegrationTests/TestResults/phase3-final_net8.0_20261021130549.trx`

### Test kapsamı

136 birim = önceki 56 + **80 yeni recurrence vakası**. Yeni kapsam: beş sıklık, Interval 2/3/int.MaxValue, açık beklenen tarih dizileri, 30/31 Ocak, artık ve artık olmayan Şubat, 29 Şubat yearly ve 2100, özel/null anchor, ilk ayda tek ödeme, EndDate/StartDate/aralık dahil sınırları, boş aralık sonucu, pasif plan, validation, null model, DateOnly Min/Max ve taşma, son döneme matematiksel atlama, geniş seyrek aralık, 10.000/10.001 sonuç sınırı, next dahil davranışı, benzersizlik/kronolojik sıra, deterministik sonuç, modelin değişmemesi ve Application DI erişimi. Büyük iş yüklerinde tam tarih dizileri bağımsız BCL takvim beklentileriyle karşılaştırıldı; yalnızca boşluk/sayı kontrolü yapılmadı.

42 entegrasyon = önceki 37 + **5 yeni gerçek SQL Server vakası**. Aylık 31 Ocak, açık AnchorDay=31, yıllık 29 Şubat, iki günlük leap geçişi ve iki haftalık yıl geçişi planları SQL Server'a kaydedildi. ChangeTracker temizlenip AsNoTracking ile yeniden okunarak StartDate/EndDate/interval/enum/anchor round-trip, SQL kolonlarının gerçek `date` tipi, beklenen recurrence tarihleri/next, rowversion'ın korunması ve occurrence yazılmaması doğrulandı. Testcontainers.MsSql SQL Server 2022 Linux kullanıldı; SQLite/EF InMemory yoktur. Tüm eski ödeme CRUD/izolasyon/concurrency/ProblemDetails testleri de geçti.

### Performans değerlendirmesi

Son Debug test çalıştırmasında, ısınma amaçlı doğrulama çağrısından sonra 100 hesaplamanın Stopwatch ölçümü:

| İş yükü | Sonuç sayısı | 100 çağrı toplam | Çağrı başına ortalama |
| --- | --- | --- | --- |
| 2026-01-01–2035-12-31 aylık (10 yıl) | 120 | 0,511 ms | 0,0051 ms |
| 2026-01-01–2055-12-31 yıllık (30 yıl) | 30 | 0,154 ms | 0,0015 ms |
| 2026-01-01–2030-12-31 günlük (5 yıl) | 1826 | 9,548 ms | 0,0955 ms |

Bunlar bu makinedeki test çıktılarıdır; Debug/JIT/GC ve eşzamanlı makine yükünden etkilenir, üretim kapasitesi veya güvenilir mikro benchmark iddiası değildir. Zamana bağlı kırılgan test assertion'ı eklenmedi. Yıl 1'de başlayan günlük plandan yıl 9999'un son üç günü doğrudan bulundu; 3,6 milyon geçmiş gün dolaşılmadı. Büyük int.MaxValue interval bütün sıklıklarda başlangıçtan sonra null üretti. Tam DateOnly aralığındaki seyrek yıllık plan 9999 tarih ile sınır içinde kaldı; yoğun günlük plan limit hatasıyla kontrollü durdu.

Next O(1), liste O(min(sonuç, 10.001)), bellek sonuç sınırıyla kısıtlıdır; ek veritabanı sorgusu ve geleceğe toplu kayıt üretimi yoktur. Küçük planlar için baştan 10.000 elemanlık buffer ayrılmaz; List kapasitesi sonuçla büyür.

### Sınırlamalar ve duruş

- Faz 3 için açık hata, warning veya atlanan test yoktur; başarısız test turu olmadı.
- Tek çağrıda en fazla 10.000 sonuç; daha geniş yoğun aralıklar caller tarafından bölünmelidir. Sonuçlarda streaming/pagination yoktur.
- Gregorian takvim tarihleri hesaplanır; saat dilimi, iş günü/tatil, bildirim saati veya occurrence durumları bu servisin konusu değildir.
- Pasif planlar için geçmiş tarih listesi de üretilmez. AnchorDay sözleşmesi artık dokümante edilmiştir; eski kayıtlar otomatik olarak değiştirilmedi.
- PaymentOccurrence paid/skipped/pending işlemleri eklenmedi; bunlar Faz 4 kapsamındadır.

**Duruş:** Faz 3 tamamlandı. Faz 4'e açık kullanıcı onayı olmadan başlanmayacak.

## Faz 4 — Payment Occurrences & Payment History

Tarih: 21 Ekim 2026 (Europe/Istanbul). Durum: **Faz 4 tamamlandı. Faz 5 başlatılmadı; açık onay bekleniyor.**

### İnceleme ve plan

AGENTS.md, önceki ilerleme, PaymentPlan/PaymentOccurrence modelleri, Domain recurrence servisi, Application CRUD sözleşmeleri, controller/hata yönetimi, SQL konfigürasyonları/InitialCreate unique-index/rowversion ve Testcontainers testleri incelendi. Başlangıç çalışma ağacı temizdi. Mevcut yapı ve API'ler korundu; .NET 8.0.426 SDK, net8.0/C# 12, EF Core 8.0.21 ve SQL Server kullanıldı. Yeni NuGet paketi veya SDK yükseltmesi yoktur.

Plan: Domain durum kuralları → Application DTO/validation/use case → atomik SQL store → üç HTTP ucu → Domain/SQL/yarış/rollback testleri → Kestrel doğrulaması → dokümantasyon ve duruş.

### Endpoint'ler ve davranışlar

| Uç | Sonuç |
| --- | --- |
| PUT /api/payments/{paymentId}/occurrences/{dueDate}/status | Yeni kayıt/güncelleme/idempotent tekrar/reset için 200 DTO; validation 400, sahiplik/yokluk 404, conflict/pasif plan 409 |
| GET /api/payments/{paymentId}/occurrences | Kayıtlı son durum geçmişi; FromDate/ToDate/Status ve bounded pagination; 200 PagedResult |
| GET /api/payments/{paymentId}/occurrences/{dueDate} | Kayıt varsa DTO; aktif planın geçerli takvim tarihinde yoksa sanal Pending; geçersiz tarih 400 |

Domain PaymentOccurrence.Create/ChangeStatus altı temel durum geçişini uygular. Paid pozitif ve SQL decimal(19,4) sınırlarında ActualAmount ister; eksik tutar varsayılan plan tutarına çevrilmez. Pending/Skipped tutar kabul etmez, PaidAtUtc null olur. PaidAtUtc/CreatedAtUtc sunucu UTC zamanıdır; istemci bunları belirleyemez. Aynı Paid/Skipped durum+tutar tekrarı Id/CreatedAtUtc/PaidAtUtc/rowversion değiştirmez; Paid tutarı düzeltilirse güncel token gerekir ve PaidAtUtc yeni işlemin zamanıdır.

Yeni/değişen occurrence tarihi `OccurrenceScheduleRules` üzerinden mevcut IPaymentRecurrenceCalculator.GetNextOccurrence ile doğrulanır; ikinci bir takvim algoritması yoktur. Geçerli ay sonu/artık yıl ve StartDate/EndDate sınırları aynen kullanılır. Bir tarihteki yazma başka tarihleri değiştirmez. Pasif planın kayıtlı geçmişi okunur; bütün status yazmaları 409 ile engellenir. Plan takvimi sonradan değişirse önceki kayıtlar okunur; artık geçerli olmayan tarihe değişiklik 400 olur. Pasif planda eksik kayıt için sanal Pending üretilmez (404).

### Pending, geçmiş ve concurrency kararları

- Gelecekteki bütün Pending tarihleri DB'ye yazılmaz. Aktif/geçerli tarihte GET ve kayıtsız Pending PUT sanal DTO döndürür.
- Paid/Skipped kayıt oluşturur. Farklı duruma/tutara geçiş mevcut occurrence'ın canonical Base64 rowVersion değerini ister; eksik/eski token 409, bozuk token 400'dür. Parent plan token'ı kullanılmaz.
- Aynı durum+tutar tekrarı, token eski/eksik olsa da yazma yapmaz. Aynı tarihe eşzamanlı aynı Paid istekleri tek kayıt ve aynı sonucu, farklı durumlar 200/409 üretir.
- Pending'e dönüş güncel token ile kaydı siler; tekrar reset kayıtsız Pending üzerinde no-op olur. Legacy Pending satırları otomatik topluca temizlenmez.
- Silme, önceki durum/tutar/ödeme zamanı/oluşturma kimliğini kaldırır. Ayrı AuditLog olayı yazılmaz; bu append-only audit veya tüm geçişlerin işlem defteri değildir. Çok geç gelen tokensız eski oluşturmayı reset sonrası yeni istekten ayıran kalıcı idempotency anahtarı yoktur. Plan adı/para birimi snapshot'ı da tutulmaz. Bu sınırlar API.md'de açıklandı.
- Parent, kullanıcı ve plan Id'siyle parametreli UPDLOCK/HOLDLOCK sorgusunda doğrulanıp Serializable transaction süresince kilitlenir. Recurrence doğrulaması, mevcut satır okuması ve yazma aynı transaction içindedir; eşzamanlı plan değişikliği/silme ile sıralanır.
- Retry destekli EF execution strategy transaction'ın tamamını sarar; her denemede tracked durum temizlenir. SQL unique 2601/2627 ve FK 547 güvenli conflict'e çevrilir; rowversion EF update/delete koruması sürer. Başarısız yazma geri alınır; ham SQL hatası response'a girmez.
- Geçmişte AsNoTracking, sabit üç sorgu (sahiplik/count/page), DueDate DESC/Id ASC ve plan/tarih indeksi kullanılır; N+1 veya toplu gelecek üretimi yoktur. Count/page tek snapshot değildir; aynı plan yazmaları DB üzerinde serialize edilir. Yük testi/üretim throughput iddiası yoktur.

### Dosyalar ve veritabanı etkisi

Yeni dosyalar:

- `src/PayAhead.Domain/PaymentOccurrence.Behavior.cs` (entity davranışı ve OccurrenceScheduleRules)
- `src/PayAhead.Application/Occurrences/Contracts.cs`, `Validators.cs`, `OccurrenceService.cs`
- `src/PayAhead.Infrastructure/OccurrenceStore.cs`
- `src/PayAhead.Api/Controllers/OccurrencesController.cs`, `OccurrenceSchemaFilter.cs`
- `tests/PayAhead.UnitTests/OccurrenceTests.cs`
- `tests/PayAhead.IntegrationTests/OccurrenceApiTests.cs`

Değişen dosyalar: Domain `Entities.cs` (partial), Application/Infrastructure `DependencyInjection.cs`, API `Program.cs` (enum ve schema kaydı), `ApiExceptionHandler.cs` (güvenli Domain validation açıklaması), `docs/API.md`, `ARCHITECTURE.md`, `DATABASE.md`, `DEVELOPMENT.md`, bu ilerleme dosyası, `README.md`, `AGENTS.md`.

Şema değişmedi; yeni migration yoktur. Mevcut unique(PaymentPlanId,DueDate), FK, decimal(19,4), state check constraint ve rowversion zaten yeterliydi. `ef migrations has-pending-model-changes` model farkı olmadığını doğruladı. Mevcut migration/snapshot ve geliştirme/üretim kayıtları değiştirilmedi veya silinmedi. Testler sadece kendi rastgele veritabanlarını oluşturup temizledi.

### Gerçek komut ve sonuçlar

Dotnet komutları proje `.tools/dotnet/dotnet.exe` SDK **8.0.426** ile çalıştırıldı; DOTNET_CLI_HOME=.tools/cli, DOTNET_ROOT=.tools/dotnet. Docker bin dizini yalnızca oturum PATH'ine eklendi. PAYAHEAD_TEST_SQLSERVER kaldırılarak Testcontainers yolu kullanıldı; Engine 29.9.1 Linux çalışıyordu.

| Kontrol | Gerçek sonuç |
| --- | --- |
| İlk yeni occurrence unit testi | 29 geçti |
| İlk yeni occurrence SQL test turu | 31 geçti |
| Son dotnet restore PayAhead.sln | Başarılı; tüm projeler güncel |
| Son dotnet build PayAhead.sln --no-restore | Başarılı; 0 uyarı, 0 hata |
| Son dotnet test PayAhead.sln --no-build --logger trx;LogFilePrefix=phase4-final | **165 unit + 77 integration başarılı; 0 başarısız, 0 atlanan** |
| dotnet ef migrations has-pending-model-changes --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build | Model değişikliği yok |
| Kestrel + gerçek Compose SQL Server, son tekrar | readiness 200; plan POST 201; virtual GET/Paid/repeat/Skipped/history/reset/boş history 200; stale PUT 409; invalid due GET 400; OpenAPI 200 |

Son TRX: `tests/PayAhead.UnitTests/TestResults/phase4-final_net8.0_20261021132459.trx` ve `tests/PayAhead.IntegrationTests/TestResults/phase4-final_net8.0_20261021132546.trx`; Git dışında tutulur.

### Test kapsamı

165 unit = önceki **136** + yeni **29**. Yeni testler altı durum geçişi, PaidAtUtc oluşturma/temizleme, tutar/null/precision, invalid status/token/UTC, ay sonu/artık yıl/invalid recurrence, sanal Pending, tekrarda tek kayıt/zamanın korunması, eksik/eski version, reset'in idempotence'ı, pasif/değişmiş plan okuma-yazma politikası ve liste doğrulamasını kapsar. Birim memory store SQL kanıtı sayılmadı.

77 integration = önceki **42** + yeni **35**. Gerçek SQL Server Testcontainers üzerinde insert/update/reset-delete, yalnızca ilgili tarihin değişmesi, idempotent tekrar, date/status/range/pagination filtreleri, kullanıcı izolasyonu/anonim erişim, invalid route/body/overposting, iki eşzamanlı aynı veya farklı status insert'i, eski token ve iki yazar, occurrence rowversion'ın ham EF düzeyinde çatışması, gerçek unique/FK ihlali ve store tarafından güvenli conflict'e eşlenmesi, mevcut plan delete koruması, SQL yazımı sonrası enjekte edilen hata ile transaction rollback, plan lock yarışında takvim değişikliği/silme, takvim editinden sonra geçmişin okunması ve OpenAPI required status doğrulandı. SQLite/EF InMemory yoktur. Bütün önceki CRUD ve recurrence testleri de geçti.

Parent yarış testleri DbCommandInterceptor sinyali ile gerçek SQL kilidinin önünde koordine edilir; rastgele sleep'e dayanmaz. Rollback testi SaveChanges sonrası, commit öncesi hata üretir ve DB'de satır kalmadığını doğrular. Unique/FK hata çeviri testleri gerçek constraint ihlali üretir; mocked exception kullanılmaz.

### Kestrel doğrulaması ve ara hatalar

Git dışı `.tools/phase4-smoke.ps1`, Compose SQL Server'da sadece `PayAheadSmoke_<guid>` veritabanı oluşturdu, mevcut migration'ı uyguladı, geliştirme test kimliğini seed etti ve API'yi gizli ayrı Kestrel sürecinde başlattı. Üç endpoint, durum geçişleri, idempotence, stale token, invalid tarih ve Swagger gerçek HTTP ile doğrulandı. Betik kendi API sürecini ve sadece kendi geçici veritabanını temizledi. Gerçek parolalar kaynak koduna veya çıktıya yazılmadı.

İlk unit derlemesinde FluentValidation ValidateAsync için belirsiz target-typed new açık request tipine çevrildi. Son tüm testler geçti. Kestrel betiğinin ilk denemesinde PowerShell stdin UTF-8 BOM, sqlcmd seed/cleanup girdi hatası oluşturdu; HTTP akışı o denemede başarılı sayılmadı. Adı ayrıca doğrulanan yalnızca o denemenin geçici DB'si temizlendi. Betik BOM'suz SQL dosyalarını container'a kopyalayıp sqlcmd -i ile okuyacak şekilde düzeltildi; sonraki Kestrel akışı ve cleanup exit code 0 ile tamamlandı. Ürün migration veya SQL model hatası değildi.

### Sınırlamalar ve duruş

Faz 4 için açık derleme/test/HTTP hatası veya atlanan test yoktur. Kayıtlı durum geçmişi immutable audit değildir; reset silme, kalıcı idempotency anahtarı ve para birimi snapshot'ı sınırları yukarıda ve API.md'de açıklanmıştır. Üretim giriş/JWT Faz 6'da; şu an yalnızca Development kimliğiyle API denemesi yapılır, Production/Staging koruması değiştirilmedi. Hesaplanan takvim/özet, bildirim veya başka Faz 5+ işlevi eklenmedi.

**Duruş:** Faz 4 tamamlandı. Faz 5 için açık kullanıcı onayı bekleniyor.

## Faz 5 — Dashboard, Upcoming Payments & Calendar API

Tarih: 21 Ekim 2026 (Europe/Istanbul). Durum: **Faz 5 tamamlandı. Faz 6 authentication başlatılmadı; açık onay bekleniyor.**

### İnceleme ve plan

AGENTS.md, önceki ilerleme, PaymentPlan/PaymentOccurrence/UserSettings, mevcut recurrence, Faz 4 durum politikası, DTO'lar, Development identity, SQL indeksleri ve test altyapısı incelendi. Başlangıç çalışma ağacı temizdi. SDK 8.0.426, net8.0/C# 12, EF Core 8.0.21, Docker Engine 29.9.1 Linux korundu. Yeni NuGet bağımlılığı veya sürüm yükseltmesi yoktur.

Plan: ortak Application okuma DTO/validation/projeksiyonu → kullanıcıya özel toplu SQL store → üç controller ucu → tarih/status/currency/timezone/limit ve SQL sorgu sayısı testleri → tüm regresyon + gerçek Kestrel → dokümantasyon ve duruş.

### Endpoint ve DTO'lar

| Uç | Özellik |
| --- | --- |
| GET /api/payments/upcoming | from/to, varsayılan Pending status, currency, deterministic sıralama ve bounded pagination |
| GET /api/dashboard/summary | aktif plan ve durum sayıları, upcoming/paid currency toplamları, nextUpcomingPayment |
| GET /api/calendar | zorunlu year/month; isteğe bağlı status/currency; ödeme içeren günlere gruplama |

DTO'lar: ProjectedPayment, ProjectionContext, CurrencyTotal, UpcomingPaymentsResponse, DashboardSummaryResponse, CalendarDayResponse ve CalendarResponse. Response entity/UserId içermez. ProjectedPayment'ta plan Id/ad, DueDate, effective Amount, CurrencyCode, Category, Frequency, Status, ActualAmount ve UTC PaidAtUtc bulunur. Context from/to, kullanıcı yerel today, timezone Id ve fallback işareti taşır.

### Hesaplama kararları

- Aktif planların tarihleri mevcut IPaymentRecurrenceCalculator ile hesaplandı; recurrence/anchor/interval/end/leap davranışı değiştirilmedi. Kayıt yoksa Pending, kayıt varsa kaydedilmiş durum; plan+tarih anahtarıyla tekilleştirme.
- Pasif veya takvimi değişmiş planların aralıktaki kayıtlı Paid/Skipped durumları korunur; yeni Pending yalnızca şu an aktif ve güncel takviminden gelir. Pasif legacy Pending marker'ı veya aktif planın artık takvim dışı Pending marker'ı gösterilmez.
- Paid satırının Amount'u ActualAmount; Pending/Skipped Amount'u güncel plan tutarıdır. Paid dashboard toplamı ActualAmount'tır. Skipped ve Paid yaklaşan borca girmez; finansal toplamlar sadece decimal ile, CurrencyCode'a göre ayrı gruplar halinde hesaplanır.
- PendingPaymentCount aralıktaki bütün Pending; UpcomingPaymentCount bunun kullanıcı yerel bugün ve sonrasındaki alt kümesidir. UpcomingTotalsByCurrency ve NextUpcomingPayment aynı yaklaşan alt kümeyi kullanır. Geçmiş seçilince iki count birbirinden ayrılabilir. ActivePaymentPlanCount tüm kullanıcı IsActive flag'lerini sayar, aralığa bağlı değildir.
- Calendar sadece ay içindeki ödeme içeren günleri döndürür. PaymentCount görünür bütün durumları, TotalsByCurrency yalnızca o günün Pending borcunu sayar; geçmiş ayda gecikmiş Pending dahildir. Her gün içi sıra ortak deterministik sıradır.
- Sıra DueDate ASC, ordinal PaymentName ASC, .NET Guid ASC. Upcoming status ve pagination birleşimden sonra uygulanır; currency SQL'e de itilerek okuma hacmi azaltılır.
- TimeProvider bir kez okunur. UserSettings.TimeZoneId ile yerel bugün; eksik/boş/geçersiz/desteklenmeyen değerde UTC fallback ve UsedTimeZoneFallback=true. Sunucu timezone'u kullanılmaz. Default from=today, to=from+30; dahil sınırlar 31 tarih demektir. Tam 7 gün +6, tam 30 gün +29 ile istenir. DateOnly.MaxValue'da varsayılan bitiş kısaltılır.
- Modelde deactivation zamanı veya currency snapshot yoktur. Cutoff/orijinal para birimi tahmin edilmedi; aralıktaki tüm kayıtlı Paid/Skipped tarihleri görünür ve metadata/currency güncel parent'tan gelir. Tarihsel currency değişmişse orijinal para birimi doğruluğu iddia edilmez. Bu sınır API.md/README'de belirtildi.

### Performans ve veri erişimi

En fazla 366 dahil gün, 1.000 ilgili plan, 50.000 kaynak occurrence ve 50.000 birleşik sonuç. Sınır aşımı 400 Domain ProblemDetails; sessiz kısmi liste/toplam yoktur. Plan başına Domain 10.000 sınırı da korunur; bu pencerede günlük plan en fazla 366 üretir. Status veya pageSize=1 korumayı atlatmaz.

SQL store kullanıcıya filtreli AsNoTracking sorguları kullanır. Timezone, ilgili planlar ve occurrence'lar topludur; occurrence sorgusu yüklenen plan Id'leri ve sahiplik join'i ile sınırlandırılır. Böylece sorgular arasındaki yeni plan kayıtları dictionary dışı kayda yol açmaz. Upcoming/calendar üç, dashboard aktif sayımı ile dört sorgudur; boş ilgili planlarda occurrence sorgusu atlanır. Aktif sayımı onu kullanmayan iki uçtan kaldırıldı. N+1 yoktur. Mevcut UserId/active/start ve plan/dueDate unique indeksleri değerlendirildi; ölçümsüz yeni indeks veya migration eklenmedi. Okumalar tek point-in-time snapshot değildir; eşzamanlı yazmalarda yeniden GET gerekebilir.

Yerel Debug birim ölçümleri (SQL süreleri değil; tek çağrı, input planları ölçümden önce hazır):

| Günlük plan × dahil gün | Sonuç | Süre | Ölçüm thread allocation |
| --- | --- | --- | --- |
| 10 × 30 | 300 | 0,338 ms | 103.728 byte |
| 10 × 365 | 3.650 | 1,519 ms | 1.089.888 byte |
| 100 × 30 | 3.000 | 1,403 ms | 978.784 byte |
| 100 × 365 | 36.500 | 25,700 ms | 13.864.856 byte |
| 1000 × 30 | 30.000 | 18,439 ms | 9.439.344 byte |
| 1000 × 365 | 50.000 sınırında açık hata | 16,256 ms | 13.561.624 byte |

Allocation ölçümü aynı managed thread'de kalındığında kaydedilir; peak heap veya gerçek retained memory değildir. JIT/GC/Debug ve test makinesi yükünden etkilenir; izole benchmark veya üretim kapasitesi iddiası yoktur. Geçici snapshot/dictionary/DTO/sıralama buffer'ları da bellek kullanır; sınırlar bu hacmi kontrol eder. Kırılgan süre/bellek eşik assertion'ı eklenmedi.

### Dosyalar ve veritabanı etkisi

Yeni dosyalar:

- `src/PayAhead.Application/Overview/Contracts.cs`, `Validators.cs`, `OverviewService.cs`
- `src/PayAhead.Infrastructure/OverviewStore.cs`
- `src/PayAhead.Api/Controllers/OverviewController.cs`
- `tests/PayAhead.UnitTests/OverviewTests.cs`
- `tests/PayAhead.IntegrationTests/OverviewApiTests.cs`

Değişenler: Application/Infrastructure `DependencyInjection.cs`, API `Program.cs` (Swagger açıklaması), `docs/API.md`, `ARCHITECTURE.md`, `DEVELOPMENT.md`, bu dosya, `README.md`, `AGENTS.md`. Calendar year/month gerekli alanları ve upcoming defaults OpenAPI metadata'sında belirtildi. Önceki controller, Domain recurrence/occurrence, migration ve mevcut veriler değiştirilmedi. Okuma servisleri hiçbir occurrence/settings kaydı oluşturmaz. Model fark kontrolünde değişiklik yok; yeni migration gerekmedi.

### Gerçek komut ve sonuçlar

Komutlar proje `.tools/dotnet/dotnet.exe` SDK 8.0.426 ile; DOTNET_CLI_HOME=.tools/cli, DOTNET_ROOT=.tools/dotnet ve Docker bin dizini işlem PATH'inde çalıştırıldı. PAYAHEAD_TEST_SQLSERVER kaldırılarak gerçek Testcontainers SQL Server kullanıldı.

| Kontrol | Son sonuç |
| --- | --- |
| İlk OverviewTests turu | 38 başarılı |
| İlk OverviewApiTests SQL turu | 28 başarılı |
| dotnet restore PayAhead.sln | Başarılı; tüm projeler güncel |
| dotnet build PayAhead.sln --no-restore | Başarılı; 0 warning, 0 error |
| dotnet test PayAhead.sln --no-build --logger trx;LogFilePrefix=phase5-verified | **203 birim + 105 entegrasyon geçti; 0 başarısız, 0 atlanan** |
| ef migrations has-pending-model-changes | Model değişikliği yok |
| 10/100/1000 plan SQL command sayımı | Her hacimde upcoming 3, dashboard 4; N+1 yok |
| Kestrel + gerçek Compose SQL Server | Üç GET/health/OpenAPI 200; invalid month 400; Paid actual tutar, USD Pending borcu ve Skipped hariç tutulması doğrulandı |

Son TRX raporları: `tests/PayAhead.UnitTests/TestResults/phase5-verified_net8.0_20261021135535.trx`, `tests/PayAhead.IntegrationTests/TestResults/phase5-verified_net8.0_20261021135627.trx`. Git dışında tutulur.

203 unit = önceki 165 + yeni 38: beş recurrence türü/interval, ay sonu/leap, merge/duplicate prevention, actual/pending tutar, currency ayrımı, pasif/değişmiş takvim geçmişi, overdue farkı, tam 7 günlük pencere, calendar grouping, sorting/pagination, boş dashboard, timezone/fallback, invalid range/status/currency/year/month, DateOnly.MaxValue ve kapasite/performance senaryoları.

105 integration = önceki 77 + yeni 28: üç endpoint, gerçek SQL decimal(19,4), kayıtlı Paid/Skipped ve hesaplanan Pending birleşimi, pagination/filtre, timezone UserSettings ve fallback, cross-user/anonim erişim/query UserId'nin etkisizliği, validation ProblemDetails, pasif ve değişmiş takvim geçmişi, boş kullanıcı/9999 Aralık, 10/100/1000 sabit query sayısı, SQL plan ve birleşik sonuç sınırları, OpenAPI ve overdue/pending ayrımı. Bütün eski CRUD/recurrence/occurrence/concurrency/transaction testleri de çalıştı ve geçti. SQLite/EF InMemory yoktur; birim memory store SQL kanıtı sayılmadı.

Kestrel için Git dışı `.tools/phase5-smoke.ps1` yalnızca kendi `PayAheadSmoke_<guid>` DB'sini oluşturdu, mevcut migration'ı uyguladı ve UTC settings/test kullanıcısı seed etti. API gizli ayrı süreçte çalıştı; plan create 201 ve status PUT 200 sonrasında upcoming/dashboard/calendar/Swagger gerçek HTTP ile test edildi. Farklı TRY/USD toplamları, ActualAmount, Skipped sonrası borcun sıfırlanması ve timezone metadata'sı doğrulandı. Sonuç/cleanup exit code 0; yalnızca testin kendi API süreci ve geçici DB'si temizlendi. Gerçek parolalar kaynak/çıktıya yazılmadı.

### Ara düzeltmeler ve sınırlar

İlk derlemede LINQ query sözdiziminde contextual `from` parametresi karşılaştırması belirsizlik yarattı; parametre fromDate/toDate olarak açık adlandırıldı. Source okuması yüklenen plan Id'leriyle sabitlendi. İlk çalışan modelde ortak snapshot aktif sayımını her uçta yapıyordu; son sorgu incelemesinde upcoming/calendar sayımı kaldırıldı ve tüm regresyon/Kestrel yeniden çalıştırılarak üç/dört query doğrulandı. Başarısız test sonucu gizlenmedi; test turlarının tamamı geçti.

Faz 5 için açık build/test/HTTP hatası veya atlanan test yoktur. Tarihsel para birimi/deactivation snapshot'ı, snapshot tutarlı çoklu read ve üretim yük testi sağlanmamıştır; mevcut sınırlar açıkça belgelendi. Auth/JWT/refresh veya Faz 6+ özelliği eklenmedi. Mevcut Development identity production engeli korundu.

**Duruş:** Faz 5 tamamlandı. Faz 6 authentication için açık kullanıcı onayı bekleniyor.

## Faz 6 — Authentication, Authorization & Security (21 Ekim 2026)

Kullanıcının ekli Faz 6 talebiyle yalnızca authentication/security uygulandı. Önce AGENTS, mevcut katmanlar, User/RefreshToken, API pipeline, migration ve testler incelendi. Plan: Application auth sözleşmeleri ve servisleri; Identity hash/SQL store; JWT/proxy/rate limit/controller; güvenli ek migration; birim ve gerçek SQL regresyonları; Kestrel akışı; dokümantasyon. Faz 7 başlatılmadı.

### Tamamlanan davranış

- POST /api/auth/register (201, otomatik login yok), login (200), refresh (200), logout (204), GET /api/users/me (200).
- Register email Trim/ToUpperInvariant, NormalizedEmail unique yarış koruması, parola 12–128, ad 1–100, başlangıç UserSettings ile atomik insert. Duplicate 409; hash/parola yanıtta yok.
- ASP.NET Identity PasswordHasher V3, PBKDF2, 210.000 iteration, salt ve verify/rehash; özel parola hash algoritması yok. Bilinmeyen hesap için dummy verify, login genel 401. Beş yanlış parola sonrası 15 dakika SQL hesap kilidi.
- JWT HS256, imza/algoritma/issuer/audience/lifetime; sub/iss/aud/iat/nbf/exp/jti, PII yok. Access varsayılan 15 dakika, skew 15 saniye. Production/Staging missing/weak secret başlangıç hatası; anahtar kaynakta yok. Development eksik anahtarda süreç ömürlü CSPRNG anahtar.
- Refresh 48 CSPRNG byte, DB SHA-256 hex hash, varsayılan 30 gün, aile ve atomik rotasyon. Kullanıcı UPDLOCK/HOLDLOCK ile birden fazla API örneğinde aynı token yarışında tek kazanan; child insert + parent revoke self-FK transaction içinde. Reuse aileyi kapatır; yarışın kaybedeni kazanan child token değerini de iptal eder. İptal commit edildikten sonra 401. Enjekte edilen ikinci SaveChanges hatası ilk insert dahil rollback ile doğrulandı.
- Logout aileyi iptal eder, tekrarı/bilinmeyen doğru biçimli token 204. Access JWT ömrü dolana kadar geçerli kalır; pasif/silinmiş kullanıcı her doğrulamada SQL üzerinden hemen engellenir. Ayrı login aileleri birbirini kapatmaz.
- Tüm mevcut ödeme/occurrence/overview controller yetkilendirmeleri korunur; gerçek JWT sub kullanıcı sahipliğini belirler. Başka kullanıcı kaydı 404. İstek başına aktif kullanıcı için ek SQL sorgusu vardır; overview veri sorgu sayısı değişmedi.
- Shipping API DevelopmentIdentity handler kaydetmez; eski Development bayrağı da kimlik sağlamaz. Production/Staging eski bayrak açıksa başlangıç hatası. Eski regresyonlar test assembly içindeki özel handler kullanır; yeni auth testleri gerçek JWT.
- Auth IP bütçesi 20/dakika, 429 ProblemDetails/Retry-After, no-store/no-cache yanıtları, güvenilir proxy IP allowlist dışında forwarded headers kapalı, production HSTS/HTTPS. Swagger bearer tanımı ve korunan uç güvenlik gereksinimi.

### Migration ve veri koruma

Yeni `20261021121138_AuthenticationSessions`: Users.FailedLoginCount/LoginLockedUntilUtc, RefreshTokens.FamilyId ve (UserId,FamilyId) index. InitialCreate değiştirilmedi. Eski token satırları silinmeden FamilyId=Id ve boş RevokedAtUtc=migration zamanı; bilinmeyen legacy lineage için yeniden login gerekir. Yeni kurulum ve eski migration + seed user/token üzerine yükseltme gerçek SQL Server üzerinde geçti. Beklenen 10 tablo ve mevcut FK/unique/decimal/rowversion regresyonları geçti. Mevcut geliştirme DB/volume verileri değiştirilmedi; yeni migration kullanımdan önce açık EF update komutuyla uygulanmalıdır. Startup otomatik migrate etmez.

### Gerçek doğrulama sonuçları

SDK `.tools/dotnet/dotnet.exe` **8.0.426**, hedef net8.0/C#12, EF Core 8.0.21. Docker Engine 29.9.1 Linux, Testcontainers SQL Server 2022. PAYAHEAD_TEST_SQLSERVER kaldırılarak testlerin kendi container kullanması sağlandı; SQLite/InMemory SQL kanıtı olarak kullanılmadı.

| İşlem | Sonuç |
| --- | --- |
| dotnet restore PayAhead.sln | Başarılı |
| dotnet build PayAhead.sln --no-restore | Başarılı, 0 warning / 0 error |
| dotnet test PayAhead.sln --no-build | **224 birim + 136 gerçek SQL entegrasyon geçti; 0 başarısız, 0 atlanan** |
| EF has-pending-model-changes | Model değişikliği yok |
| Migration fresh + legacy upgrade | Başarılı; kullanıcı/token kayıtları korunarak yükseltildi |
| git diff --check | Başarılı |
| Kestrel + Compose SQL Server | Aşağıdaki akışın tamamı geçti |

224 birim = önceki 203 + yeni 21; registration sınırları, normalizasyon, Identity salt/verify/legacy hash, JWT claims/ömür/ayarlar, zayıf/eksik key, refresh rastgeleliği/hash ve auth servis orkestrasyonu/claim sınırları. Servis memory fake testleri SQL kanıtı sayılmadı.

136 integration = önceki 105 + yeni 31; register/persistence/settings, duplicate concurrent register, validation, login generic hata/kalıcı lock/unlock, gerçek JWT me/izolasyon, rotation/reuse/independent family/logout, concurrent refresh, expired/revoked refresh, pasif/silinmiş kullanıcı, missing/malformed/invalid signature/issuer/audience/lifetime/algorithm/subject/unknown user JWT, hız sınırı ve sahte forwarded IP, missing/weak production key, Production HTTP→HTTPS yönlendirmesi, DevelopmentIdentity kapalı sınır, ikinci save failure rollback ve legacy migration. Önceki bütün payment/recurrence/occurrence/overview testleri de çalıştı.

TRX (Git dışında): `tests/PayAhead.UnitTests/TestResults/phase6-final_net8.0_20261021153230.trx`, `tests/PayAhead.IntegrationTests/TestResults/phase6-final_net8.0_20261021153330.trx`.

### Kestrel HTTP kanıtı

Git dışı `.tools/phase6-smoke.ps1` yeni rastgele PayAheadSmoke_<guid> DB üzerinde iki migration uyguladı, Kestrel başlattı; gerçek JWT dışında kimlik kullanılmadı. Token/parola çıktıya yazılmadı.

| Adım | HTTP |
| --- | --- |
| Ready / Swagger | 200 / 200 |
| JWT olmadan payments | 401 |
| İlk kullanıcı register / login / me | 201 / 200 / 200 |
| Access token ile payment create / list | 201 / 200 (1 kayıt) |
| İkinci kullanıcı register / login | 201 / 200 |
| İkinci kullanıcı ilk payment GET | 404 |
| İlk refresh rotation | 200 |
| Eski refresh reuse | 401 |
| Reuse sonrası child refresh | 401 |
| İkinci oturum logout / tekrar logout | 204 / 204 |
| Logout sonrası refresh | 401 |
| Logout sonrası henüz süresi dolmamış access ile me | 200 (belgelenen davranış) |

Smoke exit 0; sadece kendi Kestrel süreci durduruldu ve kendi geçici DB temizlendi. Mevcut container, named volume ve geliştirme verileri korundu.

### Ara hatalar ve açık sınırlar

İlk derlemede kullanılmayan constructor parametresi ve testte eksik using/target-typed validator belirsizliği düzeltildi. İlk auth SQL turu 23/25 geçti: iki test assertion hatası (aynı response stream tekrar okuma ve bağımsız iki zinciri tek sanma) düzeltildi; son tam regresyon 360/360 geçti. Başarısız sonuçlar başarı gibi raporlanmadı. Sandbox NuGet.Config/Docker erişimi için açık escalation kullanıldı. Windows script execution policy yalnızca smoke alt sürecinde Bypass ile aşıldı; sistem politikası değiştirilmedi.

Açık build/test/HTTP hatası veya atlanan test yok. Access anlık oturum iptali, çok anahtarlı JWT rotation, dağıtık IP limiter, mutlak refresh aile ömrü ve otomatik token temizliği bu fazda sağlanmadı; [SECURITY.md](SECURITY.md) içinde açıkça belgeli. Rate limiter süreç başına; üretimde edge/distributed koruma gerekir. Duplicate registration email varlığını çıkarılabilir kılar. Mevcut overview tarihsel currency/deactivation/snapshot sınırları aynen geçerlidir. MFA, parola sıfırlama/email doğrulama ve frontend eklenmedi.

Değişenler: Application/Auth; Infrastructure AuthStore/IdentityPasswordService/DI/model/migration+snapshot; API auth/users controller, TokenService/JwtSettings, AuthConfiguration, Swagger OperationFilter, Program/CurrentUser; yeni auth unit/integration testleri ve eski API test factory için test handler; README, API/ARCHITECTURE/DATABASE/DEVELOPMENT/SECURITY/IMPLEMENTATION_PROGRESS/DEPLOYMENT/PRODUCTION_CHECKLIST, AGENTS.

**Duruş:** Faz 6 tamamlandı. Faz 7 başlatılmadı; kullanıcı onayı bekleniyor.

Son production incelemesinde HTTPS port keşfine bağımlılık giderildi: varsayılan hedef 443, HttpsPort ile açık override. Production host üzerinde HTTP isteğinin HTTPS 443 hedefine 307 yönlendirildiği yeni test eklendi; ardından bütün çözüm yeniden build/test edildi. Son sonuç yukarıdaki phase6-final raporlarıdır.

## Faz 7 — Reminders, Push Notifications & Background Processing (21 Ekim 2026)

Kullanıcının ekli Faz 7 talebiyle yalnızca bu faz uygulandı. AGENTS, Faz 6 ilerleme/güvenlik, UserSettings/ReminderRule/DeviceToken/NotificationDelivery/PaymentPlan/Occurrence, recurrence, JWT ve migration geçmişi incelendi. Plan: önce auth migration + gerçek DB bütünlüğü; management API; timezone scheduling + SQL durable queue/lease; Expo ticket/receipt adapter; unit/SQL/Kestrel/worker doğrulama; dokümantasyon. Faz 8 ve React Native kodu eklenmedi.

### Mevcut geliştirme DB ve migration sonuçları

Docker Engine 29.9.1, Linux SQL Server 2022 container `payahead-phase1-sqlserver-1` healthy; localhost:14333, mevcut named volume korundu. Başlangıçta `PayAheadDockerDevelopment` geçmişinde yalnızca InitialCreate vardı; AuthenticationSessions pending listelendi. Auth migration Up incelendi: sütun/index ekleme ve legacy refresh revocation, veri silme yok. Açık EF update ile uygulandı. Users/PaymentPlans/PaymentOccurrences/RefreshTokens önce ve sonra 0/0/0/0; sütunlar/index doğrulandı, DBCC CHECKCONSTRAINTS temiz.

Yeni `20261021124526_DurableNotifications` migration üretildi; eski InitialCreate ve AuthenticationSessions dosyaları değiştirilmedi. ReminderRules.IsDeleted, device-owner alternate key, delivery device/provider/error/retry/lease/receipt/updated alanları; rule unique filter, rule/due/device unique ve claim index, status/receipt check ve composite device-owner FK eklendi. Up veri/kolon/tablo silmez, eski indeksler genişletilerek yeniden kurulur. Device tanımı bilinmeyen legacy delivery kayıtları silinmeden claim dışı tutulur; Sent eski hali korunur, diğerleri Cancelled/LegacyUnassigned olur. Seeded legacy delivery upgrade gerçek SQL testinden geçti.

SQL testleri sonrasında Faz 7 migration mevcut geliştirme DB üzerinde de uygulandı. Üç migration kayıtlı, yeni sütunlar/indexler doğrulandı, 11 etkin/güvenilir FK ve temiz DBCC CHECKCONSTRAINTS; aynı satır sayıları korundu. Otomatik startup migration eklenmedi. Önceden çalışan SQL container veya başka DB silinmedi/durdurulmadı. Down production için otomatik çalıştırılmamalıdır; yeni çoklu cihaz verisi eski unique index ile uyumsuz olabilir.

### Eklenen davranış

- Dokuz işlem: reminder POST/GET/PUT/DELETE; device POST/GET/DELETE; settings GET/PUT. Tümü gerçek JWT/aktif kullanıcı ve doğrulanmış sub sahipliği kullanır; cross-user 404, anonim 401, UserId body 400.
- Reminder DaysBefore 0–365, HH:mm (varsayılan09:00), enabled, plan başına10 limit ve SQL filtered unique duplicate koruması. Silme audit/FK için logical; silinmiş kural API listesinde yok, yeni gönderim üretmez.
- Device ios/android Expo token; own-token upsert, server Id üzerinden rotation, en fazla20 silinmemiş kayıt. Diğer hesaba token transferi yok. Yanıtta/logda token yok; DELETE token scrub+tombstone/inactive. Provider DeviceNotRegistered inactive yapar. Logout cihazı otomatik silmez; mobil istemci önce DELETE çağırmalıdır.
- Settings tr/en, ISO currency, valid timezone ve NotificationsEnabled. Legacy eksik/invalid timezone Faz5 UTC fallback ile tutarlı. Settings/kural/token değişimi kabul edilmemiş kuyruk işlerini transaction içinde iptal eder; gönderim öncesi her durumda güncel veri tekrar okunur.
- Saf NotificationPolicy + mevcut recurrence: reminder UTC penceresi now-24h ile now+30gün; due araması DaysBefore kadar genişler,365 gün önceden hatırlatma kaçırılmaz. DST gap ilk geçerli dakika, fold daha geç UTC. Sunucu local timezone kullanılmaz. Paid/Skipped/inactive/disabled/deleted/preferencesfalse/invalid device/EndDate sonrası gönderimler bastırılır.
- SQL durable queue rule/due/device unique; schedule plan lock + transaction, claim atomic UPDATE OUTPUT/UPDLOCK/READPAST/READCOMMITTEDLOCK; iki dakikalık owner lease ve stale completion fencing. Restart sonrası lease kurtarılır; cursor seek20 rules/çevrim,50 claim/çevrim,30sn poll,500 cleanup/çevrim. Worker çevrim adetleri/hata tipi loglar; hassas içerik yok.
- En fazla5 send claim, exponential backoff; Retry-After bounded1sn–1saat. HTTP timeout/bağlantı/429/5xx retryable, kalıcı hatalar Failed. Receipt ayrı15dk polling,24saat/96 claim belirsizlik sınırı. Receipt yokken push yeniden gönderilmez; açık retryable handoff hatasında yeniden eligibility+send denenir.
- IHttpClientFactory Expo adapter, dış credential/HTTPS endpoint, redirect kapalı, tüm response gövdesi dahil15sn timeout ve64KiB sınırı. Payload genel metin + opaque notificationId; ödeme tutarı/adı/email yok. Ticket AwaitingReceipt, receipt kabulü Sent (APNs/FCM handoff, cihazda gösterim değildir). Varsayılan worker disabled; enabled eksik credential startup hatası, sahte success yok.

### Doğrulama

Komutlarda yerel .NET8 SDK **8.0.426**, net8.0/C#12/EF Core8.0.21 kullanıldı. PAYAHEAD_TEST_SQLSERVER kaldırılarak gerçek Testcontainers SQL Server2022 ile çalıştırıldı; SQLite/InMemory kullanılmadı.

| Kontrol | Son sonuç |
| --- | --- |
| dotnet restore PayAhead.sln | Başarılı, tüm projeler güncel |
| dotnet build PayAhead.sln --no-restore | Başarılı,0 warning/0 error |
| dotnet test PayAhead.sln --no-build | **289 birim + 175 gerçek SQL entegrasyon geçti; 0 başarısız, 0 atlanan** |
| ef migrations has-pending-model-changes | Model değişikliği yok |
| SQL fresh + legacy upgrade | Başarılı |
| Geliştirme DB auth + notification migration | Başarılı, veriler/volume korundu |
| Kestrel gerçek SQL/JWT | Tüm istenen HTTP adımları geçti |
| Hosted BackgroundService + SQL + test FakePushNotificationSender | Başarılı; AwaitingReceipt kaydı ve tek send doğrulandı |
| Gerçek Expo/fiziksel cihaz teslimatı | **Çalıştırılmadı**; gerçek credential ve cihaz token sağlanmadı |

Yeni65 unit: days/time/device/settings doğrulama, same-day/custom leads, Istanbul ve DST gap/fold, timezone fallback/min date, paid/skipped/pending/inactive/disabled/enddate eligibility, backoff, Expo ticket/error/receipt sınıflaması, no fake success, Retry-After,65KiB response reddi, processor suppression/empty/timeout/receipt branch. SQL'e ait unique/lease/idempotency davranışları fake unit kanıtı olarak sunulmadı; gerçek SQL testlerindedir.

Yeni39 SQL integration: reminder CRUD/duplicate/concurrent/validation; device upsert/rotation/delete/privacy ve unique; settings/fallback/invalidzone; JWT ownership/anonymous/strict UserId; idempotent concurrent scheduling ve iki cihaz;365 lead;26 rule cursor batching; concurrent claim/lease recovery/stale completion;10 state suppression senaryosu; timezone/rule reschedule/amount generic payload; cancellation/delete; fake send+receipt durumları; retry/permanent/invalidtoken; max5/backoff; receipt polling/resend/24h expiry; cancellation fence; composite device-owner FK;400gün cleanup; legacy migration; gerçek hosted worker. Eski Faz1–6 testleri de çalıştı.

Ara turlar: ilk59 yeni unit geçti. İlk notification SQL32 testin31'i geçti; kalan test fake clock'un SQL datetime Kind=Unspecified değerini local gibi yorumlamasıydı. Test clock UTC olarak düzeltildi;38/38 SQL turu geçti, ardından hosted worker testi eklendi. Tam phase7-full288+174, phase7-final288+175 geçti. Son adapter response stream timeout/buffer düzeltmesi ve ek unit sonrasında son tam tur çalıştırıldı. Başarısız test gizlenmedi veya gerçek push gibi raporlanmadı. Sandbox NuGet/Docker erişimi açık escalation ile, smoke execution policy sadece alt süreçte Bypass ile çalıştı; sistem politikası değişmedi.

### Kestrel HTTP akışı

Git dışı `.tools/phase7-smoke.ps1`: kendi rastgele PayAheadSmoke_<guid> DB, üç migration, kendi gizli Kestrel süreci. Gerçek JWT kullanıldı, worker disabled, token/parola çıktıya yazılmadı.

| İstek | HTTP |
| --- | --- |
| /health/ready / Swagger | 200 /200 |
| Anonim GET /api/devices | 401 |
| Birinci kullanıcı register/login | 201 /200 |
| Payment POST | 201 |
| Reminder POST (3gün,varsayılan09:00) | 201 |
| Device POST (yanıtta token yok) | 200 |
| Settings PUT/GET | 200 /200 |
| Reminder GET/PUT (7gün10:30) | 200 /200 |
| İkinci kullanıcı register/login | 201 /200 |
| İkinci kullanıcı ilk plan reminder GET | 404 |
| İkinci kullanıcı ilk device DELETE | 404 |
| Sahibi reminder DELETE/device DELETE | 204 /204 |

Smoke exit0; yalnızca kendi geçici DB ve API süreci temizlendi. Worker processing ayrıca doğrudan NotificationProcessor + fake sender/SQL ile ve gerçek hosted BackgroundService + test fake sender ile doğrulandı; receipt ve retry durumları SQL'de incelendi.

### Belgeler, sınırlamalar ve duruş

README, API, ARCHITECTURE, DATABASE, DEVELOPMENT, SECURITY ve bu ilerleme kaydı güncellendi; kapsamlı NOTIFICATIONS.md eklendi. AGENTS Faz7 onay/Faz8 bekleme durumuna getirildi. Source değişimleri: Application/Notifications; Infrastructure yönetim/queue/Expo/DI/model/migration+snapshot; API controllers/worker/Program/config; domain ek alan/enum; unit/SQL testleri ve schema FK sayımı.

Gerçek Expo credential/proje APNs-FCM/physical-device doğrulaması yoktur; adapter sözleşme testleri ve fake sender bunu karşılamaz. Exactly-once veya cihazda görünme garantisi verilmez. Provider kabulü ile SQL completion/ödeme durumu arasında dış sistem yarışı vardır; tekrar/yanlış zamanda ulaşma ihtimali belgeli.24saatten eski kesinti catch-up yapılmaz. Büyük rule hacminde tarama gecikmesi, global Expo proje kotası, terminal Failed operasyonu ve backlog izlenmelidir. Logout push kaydını kendiliğinden kapatmaz.400gün delivery retention vardır; device/rule audit tombstone purge, yerelleştirilmiş push şablonları ve kişisel veri toplu silme süreci eklenmedi. İlişkili plan delete409 kuralı korundu.

**Duruş:** Faz7 backend uygulaması ve yerel doğrulamalar tamamlandı. Faz8 başlatılmadı; kullanıcı onayı bekleniyor.

Son doğrulanmış tur: `tests/PayAhead.UnitTests/TestResults/phase7-verified_net8.0_20261021160557.trx` ve `tests/PayAhead.IntegrationTests/TestResults/phase7-verified_net8.0_20261021160706.trx` (Git dışında). **464/464 başarılı; 0 failed, 0 skipped.** Son build 0 warning/0 error, model migration ile uyumlu, diff whitespace kontrolü temiz. Faz7 için açık build/test/HTTP hatası yoktur; gerçek push/provider teslimatı ve yukarıdaki operasyonel sınırlamalar doğrulanmış başarı kapsamına dahil değildir.
