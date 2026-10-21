# Veritabanı

SQL Server, EF Core 8 Fluent API ve InitialCreate üzerine eklenen migration geçmişi kullanılır. Güncel on dört tablo: Users, PaymentPlans, PaymentOccurrences, ReminderRules, UserSettings, DeviceTokens, UserSubscriptions, TrackedSubscriptions, RefreshTokens, NotificationDeliveries, AuditLogs, StorePurchases, StoreTransactions, StoreWebhookInbox.

## Faz 9 — VerifiedPremiumMembership

`20261021143405_VerifiedPremiumMembership` eski dört migration üzerine eklenir. UserSubscriptions'a IsStoreVerified (eski kayıtlar için false), AutoRenew, StoreEnvironment, şifreli ProtectedEvidence, Created/Updated/NextVerificationAtUtc ve SQL rowversion ekler. Eski üyeliklerin tarihi bilinmediğinden yeni Created/Updated alanları minimum datetime2 ile başlar; ilk gerçek doğrulama UpdatedAtUtc'yi yeniler. Legacy Active statüsü doğrulama sayılmaz.

StorePurchases immutable transaction/token bağları; StoreTransactions her yeni mağaza transaction'ı için audit; StoreWebhookInbox kriptografik doğrulamadan geçen event kuyruğudur. `(Store,TransactionId)` iki binding/audit tabloda unique, `(Store,TokenHash)` non-null filtered unique, `(Store,EventId)` inbox unique. Mağaza kimlikleri ASCII ve BIN2 case-sensitive karşılaştırılır. Purchase/transaction `(UserSubscriptionId,UserId)` composite FK üyelik sahibine bağlıdır; NoAction ilişkiler veri kaybını önler. Inbox claim index'i CompletedAtUtc/NextAttemptAtUtc/LeaseExpiresAtUtc üzerindedir.

SQL testleri boş DB migration, Faz 8 legacy kayıtla upgrade, transaction/token uniqueness, owner FK ve stale rowversion yazımını doğrular. Geliştirme DB'ye migration elle uygulandı; 5 migration ve 14 trusted FK mevcut, DBCC CHECKCONSTRAINTS hata vermedi. Önce/sonra eski tablo kayıt sayıları aynı (geliştirme DB başlangıçta boştu); dolu legacy upgrade ayrı izole SQL testinde doğrulandı. Snapshot ile bekleyen model değişikliği yok. Otomatik migration, eski migration silme veya veri temizliği eklenmedi.

```powershell
dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
dotnet test tests/PayAhead.IntegrationTests --filter FullyQualifiedName~PremiumIntegrationTests
```

Ham purchase token saklanmaz: benzersiz hash + dış keyring ile AES-GCM şifreli reconcile kanıtı kullanılır. Şifreleme/rotasyon ve inbox retry/retention [STORE_INTEGRATION.md](STORE_INTEGRATION.md).

- Kullanıcı e-postası NormalizedEmail üzerinden benzersizdir. Faz 6 Trim/ToUpperInvariant ve Identity PasswordHasher kullanır; özel parola kriptografisi yoktur.
- Ödeme tutarları decimal(19,4), takvim tarihleri SQL date, hatırlatma saati time, zaman damgaları datetime2 ve UTC sözleşmesindedir.
- PaymentPlan ve PaymentOccurrence SQL rowversion kullanır.
- PaymentPlanId + DueDate benzersizdir. Para pozitifliği, interval, tarih sırası, anchor ve enum aralıkları check constraint ile korunur.
- ReminderRule sahipliği bileşik foreign key ile PaymentPlan sahibine bağlanır. NotificationDelivery aynı kural/plan/kullanıcı üçlüsüne bağlanır.
- Refresh token hash alanı benzersiz, 64 ASCII karakter SHA-256 hex sözleşmesindedir. Ham refresh token saklanmaz.
- Push token ve token hash karşılaştırmaları büyük/küçük harf duyarlıdır.
- Silme ilişkileri NoAction kullanır: SQL Server çoklu cascade yolları engellenir; hesap silme ileride transaction ile açık sırada uygulanmalıdır.
- Kullanıcı/tarih ve iş kuyruğu indeksleri başlangıç sorgularını destekler; sonraki fazlarda gerçek sorgu planları ölçülmelidir.

CurrencyCode veritabanında üç büyük harf olarak sınırlandırılır; ISO 4217 üyeliği Faz 2 Application doğrulamasında SIX 2026-09-17 snapshot'ı ile uygulanır. Para birimleri arasında dönüşüm veya toplama yoktur. Zaman damgalarının UTC olması uygulama sözleşmesidir; SQL datetime2 Kind bilgisi saklamaz. Response mapper UTC Kind bilgisini geri ekleyerek JSON'da Z gösterir.

Faz 2 şema değişikliği gerektirmedi. Name uygulamada 150 karakterle sınırlıdır; mevcut nvarchar(200) kolonu daraltılmadı. İlişkisiz plan hard delete edilir; occurrence/reminder varsa 409 ile korunur. IsActive silinme göstergesi olarak kullanılmaz. Mevcut NoAction FK'lar silme kontrolünden sonra eklenmiş ilişkilere karşı da veriyi korur.

Migration üretme:

```powershell
dotnet ef migrations add Name --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api
```

Tasarım zamanı için `ConnectionStrings__PayAhead` zorunludur. Üretim şemasını elle değiştirmeyin; migration SQL çıktısını gözden geçirin. Geri alma veri kaybına yol açabilir; otomatik downgrade yoktur.

## Docker doğrulaması — 21 Ekim 2026

`docker/compose.yml`, SQL Server 2022 Linux imajı, named volume, healthcheck ve `unless-stopped` restart politikası kullanır. SA parolası `.env`/ortamdan gelir; varsayılan host portu 14333 yalnızca loopback'e bağlıdır. Başlatma/durdurma ve parola içermeyen bağlantı kurulum komutları README içindedir.

`PayAheadDockerDevelopment` veritabanında mevcut `20261021084857_InitialCreate` migration'ı başarıyla uygulandı; migration silinmedi veya yeniden oluşturulmadı. SQL Server sürümü 16.0.4305.5 (2022 CU27-GDR), Ubuntu 22.04.5 olarak sorgulandı. Container yeniden başlatıldıktan sonra aynı veritabanı ve tablolar korundu.

Fiziksel şema kontrolü: on iş tablosu ve `__EFMigrationsHistory`; on iş tablosunda PK; on etkin ve trusted FK; `PaymentPlans.Amount decimal(19,4)`; PaymentPlan/PaymentOccurrence üzerinde sekiz baytlık, null kabul etmeyen rowversion; `PaymentPlanId + DueDate` üzerinde unique index. SQL sistem katalogları rowversion için `timestamp` adını gösterebilir; bu SQL Server'da aynı otomatik binary concurrency türüdür, takvim zamanı değildir.

Testcontainers 4.8.1 varsayılan olarak ayrı SQL Server 2022 container'ı oluşturur. Test, `PayAheadTest_<guid>` veritabanına migration uygular ve sadece bu adı doğrulayarak kendi veritabanını temizler. Insert/query/update/delete, FK, unique ihlali, decimal round-trip, rowversion çatışması, check constraint ve transaction rollback gerçek SQL Server üzerinde doğrulanır. SQLite veya EF InMemory kullanılmaz.

## Faz 4 persistence ve transaction politikası

Yeni migration yoktur: başlangıç modelindeki PaymentOccurrence rowversion, decimal(19,4), status/state check constraint, PaymentPlanId FK ve unique(PaymentPlanId,DueDate) kullanılmaya devam eder. Eski migration/snapshot değiştirilmedi. EF model fark kontrolü değişiklik olmadığını doğruladı.

Durum yazarken plan, UserId ve Id ile parametreli SQL üzerinden UPDLOCK/HOLDLOCK ile okunur; occurrence okuma ve SaveChanges aynı Serializable transaction içindedir. Plan değişikliği/silme ve aynı plandaki occurrence yazmaları sıralanır. Mevcut occurrence'ın farklı duruma geçişinde istemcinin rowVersion'ı doğrulanır; EF update/delete kendi concurrency predicate'ini de uygular. Aynı durum/tutar tekrarında SaveChanges çağrılmaz. SQL unique 2601/2627 ve FK 547 güvenli 409'a çevrilir; hata halinde transaction geri alınır. Retry, transaction'ın tamamını kapsar; başarısız denemenin ChangeTracker durumu tekrar kullanılmaz.

Liste sorgusu kayıtlı durumları AsNoTracking ile okur; UserId parent join üzerinden her occurrence sorgusunda sınırlandırılır. Sahiplik kontrolü + count + page sabit üç sorgudur. DueDate DESC/Id ASC sırası ve tarih aralığı filtreleri mevcut plan/tarih unique indeksinden yararlanabilir; status için yeni indeks ölçüm yapılmadan eklenmedi. Count/page farklı sorgular olduğundan eşzamanlı yazmalarda snapshot tutarlılığı iddia edilmez.

Varsayılan Pending satırı eklenmez. Pending'e reset, yalnızca ilgili plan+tarih satırını siler; diğer tarihler değişmez. Eski Pending satırları otomatik topluca temizlenmez; açık ve yetkili reset ile kaldırılabilir. Kaydı silmek önceki Paid/Skipped bilgisini de kaldırır; AuditLog'a değişiklik olayı yazılmaz. Bu tablo immutable işlem defteri değildir. CurrencyCode/plan adı snapshot'ı tutulmaz; geçmiş para birimi yorumunda parent plan değişiklikleri dikkate alınmalıdır. Plan ve kullanıcı silme/export gibi genel yaşam döngüleri sonraki fazların konusudur.

## Faz 6 — Güvenli migration

AuthenticationSessions migration Users tablosuna FailedLoginCount (0), nullable LoginLockedUntilUtc; RefreshTokens tablosuna FamilyId ve (UserId,FamilyId) index ekler. Eski InitialCreate değişmedi. NormalizedEmail/TokenHash unique index, Users FK ve ReplacedByTokenId self FK korunur; yeni tablo/cascade delete yoktur.

Eski refresh aile geçmişi bilinemez. Upgrade FamilyId=Id atar, RevokedAtUtc boşsa migration zamanı doldurur; satırlar silinmez, eski oturumlar yeniden login ister. Kullanıcı/ödeme/occurrence verileri korunur. Development placeholder hash ile giriş yapılamaz; register ile ayrı hesap açın. Migration başlangıçta otomatik uygulanmaz; backup ve bakım penceresiyle EF update komutunu çalıştırın. Down eklenen sütunları düşürür; üretim rollback için backup/oturum planı gerekir.

Yeni kurulum ve InitialCreate üzerine legacy user/token içeren yükseltme gerçek SQL testinde doğrulanır. Refresh hashleri reuse tespiti için tutulur; otomatik temizleme işi yoktur. Süre ve transaction kararları [SECURITY.md](SECURITY.md) içindedir.

## Faz 7 — DurableNotifications şeması

`20261021124526_DurableNotifications` yeni migration: ReminderRules.IsDeleted; DeviceTokens (Id,UserId) alternate key; NotificationDeliveries.DeviceTokenId (nullable legacy), ProviderMessageId, LastErrorCode, NextAttemptAtUtc, LeaseOwner/ExpiresAtUtc, TicketAcceptedAtUtc, ReceiptAttemptCount, UpdatedAtUtc. Status 0–5 check ve receipt attempt nonnegative check. On tablo korunur, yeni device-owner composite FK ile 11 trusted FK vardır.

Eski rule unique index silinmemiş kayıtlara filtrelenerek yeniden kurulur. Eski rule/due delivery unique index, rule/due/device filtered unique index ile genişletilir; claim index eklenir. Up tablo/kolon/veri silmez; index/constraint değişimleri transaction içindedir. Önceki migration dosyaları değiştirilmedi. Device kimliği bulunmayan legacy delivery satırları korunur: Sent eski değeri kalır, diğerleri Cancelled/LegacyUnassigned olur; device bilinmediğinden gönderilmez. Upgrade seeded legacy delivery ile gerçek SQL testindedir. Down eski uniqueness ile uyumsuz çoklu cihaz/tombstone verisinde başarısız olabilir; production rollback için backup ve ileri düzeltme planı kullanın, otomatik down çalıştırmayın.

Faz 6 authentication migration bu faz başlangıcında mevcut `PayAheadDockerDevelopment` üzerinde uygulandı; önce/sonra Users/PaymentPlans/PaymentOccurrences/RefreshTokens sayıları 0 olarak aynı kaldı, DBCC CHECKCONSTRAINTS temizdi. Faz 7 migration sonuçları [IMPLEMENTATION_PROGRESS.md](IMPLEMENTATION_PROGRESS.md) içinde kayıtlıdır. Startup migration uygulamaz.

NotificationDelivery terminal kayıtlarında hem UpdatedAtUtc hem DueDate 400 günden eskiyse bounded cleanup vardır; future dedup satırları korunur. Reminder DELETE logical, device DELETE token scrub + inactive yapar; FK/audit tombstone satırları kalır. Raw push token gönderim için okunabilir saklanır; TLS, TDE/encrypted storage, şifreli backup ve dar DB yetkileri gereklidir. [Tam lifecycle](NOTIFICATIONS.md).

## Faz 8 — ExternalSubscriptionTracking

`20261021132035_ExternalSubscriptionTracking`: TrackedSubscriptions tablosu, server UTC timestamps/date alanları, rowversion; PaymentPlanId unique ve (PaymentPlanId,UserId) ownership FK; user/deleted/created/id index; status/trial/date/access check. ReminderRules.Target default0 (Payment), yeni SubscriptionTrial=1 ve genişleyen unique index.11 tablo/12 FK olur; eski UserSubscriptions entitlement verisi değişmez. Amount/Currency/BillingFrequency/Interval/FirstBillingDate parent PaymentPlan üzerinde tek kez saklanır, decimal(19,4) ve date korunur.

Up eski tablo/kolon/veri silmez; rule unique index default Payment hedefiyle yeniden kurulur. Önceki migration dosyaları değiştirilmedi. Phase7 seed Premium row ve payment ile upgrade gerçek SQL testinde korunarak geçti. Yeni kayıt/plan/trial rule atomik oluşturulur; update/cancel/delete eski queue iptaliyle aynı transaction içindedir. FailSave rollback ve SQL unique/owner/concurrency testleri vardır.

Abonelik delete logical, parent inactive; ödeme ve bildirim geçmişi silinmez. Generic payment PUT/DELETE bağlı/tombstone parent için409 döner. Read-only lifecycle tarihleri türetilir, otomatik state update/migration job yoktur. Down yeni tabloyu düşürür ve eski reminder uniqueness ile uyumsuz olabilir; production backup/ileri düzeltme planı gerekir. Migration uygulanabilirliği ve geliştirme DB sonucu [IMPLEMENTATION_PROGRESS.md](IMPLEMENTATION_PROGRESS.md) içindedir.
