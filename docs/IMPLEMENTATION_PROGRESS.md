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
